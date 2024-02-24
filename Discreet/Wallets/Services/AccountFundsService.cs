using Discreet.Coin.Models;
using Discreet.Common;
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
    public class AccountFundsService : IFundsService
    {
        private readonly object status_lock = new();
        private long lastSeenHeight;
        private Account account;
        private IView view;
        private bool requestPause = false;
        private CancellationToken token = default;
        private bool checkCoinbase = false;
        public ServiceState State { get; private set; }
        public Account Account { get { return account; } }

        public bool Paused { get { return State == ServiceState.PAUSED; } }
        public bool Completed { get { return State == ServiceState.COMPLETED; } }

        public long GetLastSeenHeight() => Interlocked.Read(ref lastSeenHeight);

        public AccountFundsService(IView view, Account account, bool checkCoinbase = true)
        {
            this.account = account;
            this.view = view;
            account.Syncing = true;
            this.State = ServiceState.INSTANTIATED;
            // check coinbase IFF either (1) checkCoinbase = true and DebugConfig.AlwaysCheckCoinbase = true, or (2) label has prefix "dbg"
            if (Daemon.DaemonConfig.GetConfig().DbgConfig.AlwaysCheckCoinbase.Value) this.checkCoinbase = checkCoinbase;
            else this.checkCoinbase = account.Wallet.Label.ToLower().StartsWith("dbg");
        }

        public void Interrupt()
        {
            if (Completed) return;

            lock (status_lock)
            {
                requestPause = true;
            }
        }

        public void ProcessBlocks(IEnumerable<Block> blocks)
        {
            var blockEnumerator = blocks.GetEnumerator();
            while (blockEnumerator.MoveNext())
            {
                if (token.IsCancellationRequested)
                {
                    account.Wallet.SaveKey(account.Name, lastSeenHeight);
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
                    account.Wallet.SaveKey(account.Name, lastSeenHeight);
                    return;
                }
                if (account.Wallet.SyncingService.GetLastSeenHeight() == lastSeenHeight)
                {
                    State = ServiceState.COMPLETED;
                    account.Syncing = false;
                    account.Wallet.DeleteKey(account.Address);
                    return;
                }

                var block = blockEnumerator.Current;
                ProcessBlock(block);
            }

            account.Wallet.SaveKey(account.Address, -1);
        }

        private void ProcessBlock(Block block)
        {
            if (!checkCoinbase && block.Header.NumTXs == 1 && block.Header.Version == 2)
            {
                lastSeenHeight = block.Header.Height;
                return;
            }

            (var spents, var utxos, var txs) = account.ProcessBlock(block);
            if (spents != null || utxos != null || txs != null) account.Wallet.SaveAccountFundData(account, spents, utxos, txs);

            lastSeenHeight = block.Header.Height;
        }

        public void Resume()
        {
            if (Completed) return;

            if (Paused)
            {
                lock (status_lock)
                {
                    State = ServiceState.SYNCING;
                }

                _ = Task.Run(() => StartFundsScan()).ConfigureAwait(false);
            }
        }

        public async void StartFundsScan(CancellationToken token = default)
        {
            if (this.token == default) this.token = token;
            lastSeenHeight = Serialization.GetInt64(account.Wallet.LoadKey(account.Address), 0);
            State = ServiceState.SYNCING;
            while (account.Wallet.SyncingService == null && !Completed)
            {
                ProcessBlocks(view.GetBlocks(lastSeenHeight + 1, 0));
            }
            while (lastSeenHeight < account.Wallet.SyncingService.GetLastSeenHeight() && !Completed)
            {
                ProcessBlocks(view.GetBlocks(lastSeenHeight + 1, 0));
            }

            while (lastSeenHeight != account.Wallet.SyncingService.GetLastSeenHeight() && !Completed)
            {
                await Task.Delay(100, token);
            }

            account.Wallet.DeleteKey(account.Address);
            account.Syncing = false;
            State = ServiceState.COMPLETED;

            if (account.Wallet.SyncingService.State == ServiceState.SYNCED)
            {
                ZMQ.Publisher.Instance.Publish("addresssynced", Encoding.UTF8.GetBytes(account.Address));
            }
        }
    }
}
