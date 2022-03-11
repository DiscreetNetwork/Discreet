using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    /// <summary>
    /// Holds information about 'this' local node
    /// </summary>
    public class LocalNode
    {
        /// <summary>
        /// Our local endpoint used to listening for incomming data
        /// </summary>
        public IPEndPoint Endpoint { get; set; }

        /// <summary>
        /// A flag for determining if we are in public or private mode
        /// </summary>
        public bool IsPublic { get; private set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="localEndpoint"></param>
        public LocalNode(IPEndPoint localEndpoint)
        {
            Endpoint = localEndpoint;
            IsPublic = Daemon.DaemonConfig.GetConfig().IsPublic.Value;
        }

        public void SetPublic() => IsPublic = true;
    }
}
