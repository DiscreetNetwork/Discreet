using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets.Services
{
    public interface IWalletService
    {
        public bool Paused { get; }
        public bool Completed { get; }
        public void Interrupt();
        public void Resume();
    }
}
