using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core
{
    public interface IPeerListener
    {

        Task PeerUpdatedCallback();

    }
}
