using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;

namespace Discreet.Wallets.Services
{
    internal interface IFundsService : IWalletService
    {
        public void StartFundsScan();
        public void ProcessBlocks(IEnumerable<Block> blocks);
    }
}
