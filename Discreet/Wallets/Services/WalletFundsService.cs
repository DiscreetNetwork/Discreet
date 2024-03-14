using Discreet.Coin.Models;
using Discreet.DB;
using Discreet.Wallets.Extensions;
using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.Wallets.Services
{
    public class WalletFundsService: IFundsService
    {
        protected readonly object status_lock = new();
        protected SQLiteWallet wallet;
        protected IView view;
        protected long lastSeenHeight;
        protected CancellationToken token = default;
        protected bool requestPause = false;
        protected bool checkCoinbase;

        public ServiceState State { get; protected set; }

        public bool Paused { get { return State == ServiceState.PAUSED; } }
        public bool Completed { get { return State == ServiceState.COMPLETED; } }

        public WalletFundsService(SQLiteWallet wallet, IView provider, bool checkCoinbase = true) 
        { 
            this.wallet = wallet;
            this.view = provider;
            State = ServiceState.INSTANTIATED;
            // check coinbase IFF either (1) checkCoinbase = true and DebugConfig.AlwaysCheckCoinbase = true, or (2) label has prefix "dbg"
            if (Daemon.DaemonConfig.GetConfig().DbgConfig.AlwaysCheckCoinbase.Value) this.checkCoinbase = checkCoinbase;
            else this.checkCoinbase = wallet.Label.ToLower().StartsWith("dbg");
        }

        public long GetLastSeenHeight() => Interlocked.Read(ref lastSeenHeight);

        public virtual void StartFundsScan(CancellationToken token = default) => Task.Run(async () => await StartFundsScanAsync(token)).ConfigureAwait(false);

        public virtual async Task StartFundsScanAsync(CancellationToken token = default)
        {
            if (this.token == default) this.token = token;
            lastSeenHeight = wallet.GetLastSeenHeight();
            State = ServiceState.SYNCING;
            ProcessBlocks(view.GetBlocks(lastSeenHeight + 1, 0));

            foreach (var addrstr in wallet.Accounts.Where(x => !x.Syncing).Select(x => x.Address).ToList())
            {
                ZMQ.Publisher.Instance.Publish("addresssynced", Encoding.UTF8.GetBytes(addrstr));
            }

            // successfully synced
            if (!this.token.IsCancellationRequested && !Paused)
            {
                State = ServiceState.SYNCED;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var chainHeight = view.GetChainHeight();
                        if (lastSeenHeight < chainHeight)
                        {
                            ProcessBlocks(Enumerable.Range(
                                    (int)lastSeenHeight + 1,
                                    (int)chainHeight - (int)lastSeenHeight)
                                .Select(x => view.GetBlock((long)x)));
                        }
                    }
                    catch
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    try
                    {
                        await Task.Delay(1000, token);
                    }
                    catch (TaskCanceledException e)
                    {
                        wallet.SetLastSeenHeight(lastSeenHeight);
                        State = ServiceState.COMPLETED;
                        return;
                    }
                }
            }
        }

        protected virtual void ProcessBlock(Block block)
        {
            if (block == null) throw new Exception("block was null");
            if (lastSeenHeight >= block.Header.Height) throw new Exception("already seen block");
            if (lastSeenHeight + 1 != block.Header.Height) throw new Exception("block out of order");

            foreach (var acc in wallet.Accounts.Where(x => !x.Syncing).ToList())
            {
                if (!checkCoinbase && block.Header.NumTXs == 1 && block.Header.Version == 2)
                {
                    lastSeenHeight = block.Header.Height;
                    return;
                }

                (var spents, var utxos, var txs) = acc.ProcessBlock(block);
                if (spents != null || utxos != null || txs != null) wallet.SaveAccountFundData(acc, spents, utxos, txs);
            }

            lastSeenHeight = block.Header.Height;
        }

        public virtual void ProcessBlocks(IEnumerable<Block> blocks)
        {
            var blockEnumerator = blocks.GetEnumerator();
            while (blockEnumerator.MoveNext())
            {
                if (this.token.IsCancellationRequested)
                {
                    wallet.SetLastSeenHeight(lastSeenHeight);
                    State = ServiceState.COMPLETED;
                    return;
                }
                if (requestPause)
                {
                    State = ServiceState.PAUSED;
                    requestPause = false;
                }
                if (Paused)
                {
                    wallet.SetLastSeenHeight(lastSeenHeight);
                    return;
                }
                var block = blockEnumerator.Current;
                ProcessBlock(block);
            }

            wallet.SetLastSeenHeight(lastSeenHeight);
        }

        public virtual void Interrupt()
        {
            if (Completed) return;

            lock (status_lock) 
            {
                requestPause = true;
            }
        }

        public virtual void Resume()
        {
            if (Completed) return;

            if (Paused)
            {
                lock (status_lock)
                {
                    State = ServiceState.SYNCING;
                }

                _ = Task.Run(() => StartFundsScanAsync()).ConfigureAwait(false);
            }
        }
    }
}
