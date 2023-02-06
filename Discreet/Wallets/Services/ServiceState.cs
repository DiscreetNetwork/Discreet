using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets.Services
{
    public enum ServiceState
    {
        INSTANTIATED = 0,
        PAUSED = 1,
        RUNNING = 2,
        COMPLETED = 3,
        // states used by IFundsService
        SYNCING = 4,
        SYNCED = 5,
    }
}
