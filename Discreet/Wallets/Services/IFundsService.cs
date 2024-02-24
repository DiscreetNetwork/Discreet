using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discreet.Coin.Models;

namespace Discreet.Wallets.Services
{
    internal interface IFundsService : IWalletService
    {
        public void StartFundsScan(CancellationToken token = default);
        public void ProcessBlocks(IEnumerable<Block> blocks);
    }
}
