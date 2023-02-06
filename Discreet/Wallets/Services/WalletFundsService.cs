using Discreet.Coin;
using Discreet.DB;
using Discreet.Wallets.Extensions;
using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.Wallets.Services
{
    public class WalletFundsService: IFundsService
    {
        private readonly object status_lock = new();
        private SQLiteWallet wallet;
        private IView view;
        private long lastSeenHeight;
        private bool requestPause = false;

        public ServiceState State { get; private set; }

        public bool Paused { get { return State == ServiceState.PAUSED; } }
        public bool Completed { get; private set; }

        public WalletFundsService(SQLiteWallet wallet, IView provider) 
        { 
            this.wallet = wallet;
            this.view = provider;
            State = ServiceState.INSTANTIATED;
        }

        public long GetLastSeenHeight() => Interlocked.Read(ref lastSeenHeight);

        public void StartFundsScan() => Task.Run(async () => await StartFundsScanAsync()).ConfigureAwait(false);

        public async Task StartFundsScanAsync(CancellationToken token = default)
        {
            lastSeenHeight = wallet.GetLastSeenHeight();
            State = ServiceState.SYNCING;
            ProcessBlocks(view.GetBlocks(lastSeenHeight + 1, 0));

            // successfully synced
            if (!token.IsCancellationRequested && !Paused)
            {
                State = ServiceState.SYNCED;
                while (!token.IsCancellationRequested)
                {
                    var chainHeight = view.GetChainHeight();
                    if (lastSeenHeight < chainHeight)
                    {
                        ProcessBlocks(Enumerable.Range(
                                (int)lastSeenHeight + 1,
                                (int)chainHeight - (int)lastSeenHeight)
                            .Select(x => view.GetBlock((long)x)));
                    }

                    await Task.Delay(1000, token);
                }
            }
        }

        public void ProcessBlock(Block block)
        {
            if (block == null) throw new Exception("block was null");
            var lastHeight = wallet.GetLastSeenHeight();
            if (lastHeight >= block.Header.Height) throw new Exception("already seen block");
            if (lastHeight + 1 != block.Header.Height) throw new Exception("block out of order");

            wallet.Accounts.Where(x => !x.Syncing).ToList().ForEach(x =>
                {
                    (var spents, var utxos, var txs) = x.ProcessBlock(block);
                    if (spents != null || utxos != null || txs != null) wallet.SaveAccountFundData(x, spents, utxos, txs);
                });
        }

        public void ProcessBlocks(IEnumerable<Block> blocks)
        {
            var blockEnumerator = blocks.GetEnumerator();
            while (blockEnumerator.MoveNext())
            {
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

        public void Interrupt()
        {
            lock (status_lock) 
            {
                requestPause = true;
            }
        }

        public void ProcessTransaction(FullTransaction tx)
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            if (Paused)
            {
                lock (status_lock)
                {
                    State = ServiceState.SYNCING;
                }

                _ = Task.Run(() => StartFundsScan()).ConfigureAwait(false);
            }
        }
    }
}
