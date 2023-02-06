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
        private Account account;
        private FullTransaction tx;
        private HistoryTx htx;
        private bool? success;

        public bool Paused { get; private set; }

        public bool Completed { get; private set; }

        public CreateTransactionService(Account account)
        {
            this.account = account;
        }

        private void OnSuccess(TransactionReceivedEventArgs e)
        {
            if (e.Tx.TxID == tx.TxID)
            {
                Handler.GetHandler().OnTransactionReceived -= OnSuccess;
                htx.Timestamp = DateTime.Now.Ticks;

                success = e.Success;
            }
        }

        public async Task WaitForSuccess(CancellationToken token = default)
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

                account.Wallet.SaveAccountFundData(account, Array.Empty<UTXO>(), Array.Empty<UTXO>(), new HistoryTx[] { htx });
            }
        }

        public FullTransaction CreateTransaction(IEnumerable<IAddress> addresses, IEnumerable<ulong> amounts)
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
                    (utxos, tx) = new CreateTransparentTx().CreateTransaction(account, addresses, amounts);
                    break;
                default:
                    throw new Exception("unknown transaction type");
            }

            if (utxos.Select(x => x.DecodedAmount).Aggregate((x, y) => x + y) > amounts.Aggregate((x, y) => x + y))
            {
                amounts = amounts.Append(amounts.Aggregate((x, y) => x + y) - amounts.Aggregate((x, y) => x + y));
                addresses = addresses.Append(new StealthAddress(account.Address));
            }

            htx = new HistoryTx(account, tx.TxID, utxos.Select(x => x.DecodedAmount).ToArray(),
                Enumerable.Repeat(account.Address, utxos.ToArray().Length).ToArray(),
                amounts.ToArray(),
                addresses.Select(x => x.ToString()).ToArray());
            Handler.GetHandler().OnTransactionReceived += OnSuccess;

            return tx;
        }

        public void Interrupt()
        {
            Paused = true;
        }

        public void Resume()
        {
            Paused = false;
        }
    }
}
