using Discreet.Coin;
using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Network;
using System.Threading;

namespace Discreet.Wallets.Services
{
    internal class CreateTransactionService : IWalletService
    {
        protected Account account;
        protected FullTransaction tx;
        protected HistoryTx htx;
        protected bool? success;
        protected bool relay = true;

        public bool Paused { get; protected set; }

        public bool Completed { get; protected set; }

        public CreateTransactionService(Account account, bool relay = true)
        {
            this.account = account;
            this.relay = relay;
        }

        protected virtual void OnSuccess(TransactionReceivedEventArgs e)
        {
            if (e.Tx.TxID == tx.TxID)
            {
                Handler.GetHandler().OnTransactionReceived -= OnSuccess;
                htx.Timestamp = DateTime.Now.Ticks;

                success = e.Success;
            }
        }

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

                    if (success.Value) account.Wallet.SaveAccountFundData(account, Array.Empty<UTXO>(), Array.Empty<UTXO>(), new HistoryTx[] { htx });
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

        public virtual FullTransaction CreateTransaction(IEnumerable<IAddress> addresses, IEnumerable<ulong> amounts)
        {
            if (addresses.ToList().Count != amounts.ToList().Count) throw new ArgumentException();

            IEnumerable<UTXO> utxos;
            switch (CreateTx.GetTransactionType(account.Type, addresses)) {
                case 1:
                case 2:
                    (utxos, tx) = new CreatePrivateTx().CreateTransaction(account, addresses, amounts);
                    break;
                case 3:
                    (utxos, tx) = new CreateTransparentTx().CreateTransaction(account, addresses, amounts);
                    break;
                case 4:
                    (utxos, tx) = new CreateMixedTx().CreateTransaction(account, addresses, amounts);
                    break;
                default:
                    throw new Exception("unknown transaction type");
            }

            if (utxos.Select(x => x.DecodedAmount).Aggregate(0UL, (x, y) => x + y) > amounts.Aggregate(0UL, (x, y) => x + y))
            {
                amounts = amounts.Append(utxos.Select(x => x.DecodedAmount).Aggregate(0UL, (x, y) => x + y) - amounts.Aggregate(0UL, (x, y) => x + y));
                addresses = addresses.Append(account.Type == 0 ? new StealthAddress(account.Address) : new TAddress(account.Address));
            }

            htx = new HistoryTx(account, tx.TxID, utxos.Select(x => x.DecodedAmount).ToArray(),
                Enumerable.Repeat(account.Address, utxos.ToArray().Length).ToArray(),
                amounts.ToArray(),
                addresses.Select(x => x.ToString()).ToArray());

            if (relay) Handler.GetHandler().OnTransactionReceived += OnSuccess;

            return tx;
        }

        public void Interrupt()
        {
            if (Completed) return;

            Paused = true;
        }

        public void Resume()
        {
            if (Completed) return;

            Paused = false;
        }
    }
}
