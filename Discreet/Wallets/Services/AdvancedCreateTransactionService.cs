using Discreet.Coin;
using Discreet.Network;
using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.Wallets.Services
{
    internal class AdvancedCreateTransactionService : CreateTransactionService
    {
        protected List<Account> accounts;
        protected List<HistoryTx> htxs;
        protected List<UTXO> inputs;

        public virtual async Task WaitForSuccess(CancellationToken token = default)
        {
            try
            {
                while (!token.IsCancellationRequested && !success.HasValue)
                {
                    await Task.Delay(100, token);
                }

                if (!token.IsCancellationRequested && success.HasValue)
                {
                    while (Paused && !token.IsCancellationRequested)
                    {
                        await Task.Delay(100, token);
                    }

                    if (success.Value) htxs.ForEach(x => x.Account.Wallet.SaveAccountFundData(account, Array.Empty<UTXO>(), Array.Empty<UTXO>(), new HistoryTx[] { x }));
                    Completed = true;
                }

                if (token.IsCancellationRequested)
                {
                    Completed = true;
                }
            }
            catch (TaskCanceledException)
            {
                Completed = true;
                return;
            }
        }

        protected override void OnSuccess(TransactionReceivedEventArgs e)
        {
            if (e.Tx.TxID == tx.TxID)
            {
                Handler.GetHandler().OnTransactionReceived -= OnSuccess;
                var timestamp = DateTime.Now.Ticks;
                htxs.ForEach(x => x.Timestamp = timestamp);

                success = e.Success;
            }
        }

        public AdvancedCreateTransactionService(Account account, bool relay = true) : base(account, relay)
        {
        }

        public AdvancedCreateTransactionService(IEnumerable<Account> accounts, bool relay = true) : base(accounts.FirstOrDefault(), relay)
        {
            // accounts may be from multiple wallets
            this.accounts = accounts.ToList();
            this.htxs = new();
        }

        public AdvancedCreateTransactionService(bool relay = false) : base(null, relay) { }

        public override FullTransaction CreateTransaction(IEnumerable<IAddress> addresses, IEnumerable<ulong> amounts)
        {
            return base.CreateTransaction(addresses, amounts);
        }

        public virtual FullTransaction CreateTransaction(List<UTXO> inputs, List<(IAddress, ulong)> outputs, IAddress change)
        {
            var accounts = inputs.Select(x => x.Account).Distinct().ToList();

            Account changeAccount = null;
            if (change is TAddress taddr)
            {
                changeAccount = new Account { Address = taddr.ToString(), Type = 1 };
            }
            else if (change is StealthAddress paddr)
            {
                changeAccount = new Account { Address = paddr.ToString(), Type = 0, PubSpendKey = paddr.spend, PubViewKey = paddr.view };
            }

            (_, var tx) = new CreatePureMixedTx(inputs).CreateTransaction(changeAccount, outputs.Select(x => x.Item1), outputs.Select(x => x.Item2));
            this.tx = tx;

            if (inputs.Select(x => x.DecodedAmount).Aggregate(0UL, (x, y) => x + y) > outputs.Select(x => x.Item2).Aggregate(0UL, (x, y) => x + y))
            {
                var changeAmount = inputs.Select(x => x.DecodedAmount).Aggregate(0UL, (x, y) => x + y) - outputs.Select(x => x.Item2).Aggregate(0UL, (x, y) => x + y);
                outputs.Add((change, changeAmount));
            }

            foreach (var account in accounts)
            {
                var htx = new HistoryTx(account, tx.TxID, inputs.Select(x => x.DecodedAmount).ToArray(),
                inputs.Select(x => x.Address).ToArray(),
                outputs.Select(x => x.Item2).ToArray(),
                outputs.Select(x => x.Item1.ToString()).ToArray());

                htxs.Add(htx);
            }

            if (relay) Handler.GetHandler().OnTransactionReceived += OnSuccess;

            return tx;
        }
    }
}
