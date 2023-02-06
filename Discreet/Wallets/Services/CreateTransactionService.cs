using Discreet.Coin;
using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets.Services
{
    internal class CreateTransactionService : IWalletService
    {
        private Account account;
        private HistoryTx tx;
        private bool success;

        public bool Paused { get; private set; }

        public bool Completed { get; private set; }

        public CreateTransactionService(Account account)
        {
            this.account = account;
        }

        public void CreateTransaction(IEnumerable<IAddress> addresses, IEnumerable<ulong> amounts)
        {

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
