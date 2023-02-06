using Discreet.Coin;
using Discreet.DB;
using Discreet.Wallets.Extensions;
using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public ServiceState State { get; private set; }

        public bool Paused { get { return State == ServiceState.PAUSED; } }
        public bool Completed { get; private set; }

        public AccountFundsService(IView view, Account account)
        {
            this.account = account;
            this.view = view;
            account.Syncing = true;
            this.State = ServiceState.INSTANTIATED;
        }

        public void Interrupt()
        {
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
                    Completed = true;
                    account.Syncing = false;
                    account.Wallet.DeleteKey(account.Address);
                    return;
                }

                var block = blockEnumerator.Current;
                ProcessBlock(block);
            }

        }

        private void ProcessBlock(Block block)
        {
            (var spents, var utxos, var txs) = account.ProcessBlock(block);
            if (spents != null || utxos != null || txs != null) account.Wallet.SaveAccountFundData(account, spents, utxos, txs);
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

        public void StartFundsScan()
        {
            lastSeenHeight = Serialization.GetInt64(account.Wallet.LoadKey(account.Address), 0);
            State = ServiceState.SYNCING;
            ProcessBlocks(view.GetBlocks(lastSeenHeight + 1, 0));
        }
    }
}
