﻿using System;
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
        /// Our local random generated 'NodeId'
        /// </summary>
        public NodeId Id { get; set; }

        /// <summary>
        /// Our local endpoint used to listening for incomming data
        /// </summary>
        public IPEndPoint Endpoint { get; set; }

        /// <summary>
        /// A flag for determining if we are in public or private mode
        /// </summary>
        public bool IsPublic { get; private set; } = true;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="localEndpoint"></param>
        public LocalNode(IPEndPoint localEndpoint)
        {
            Id = new NodeId();
            Endpoint = localEndpoint;
        }

        public LocalNode(IPEndPoint localEndpoint, NodeId ID)
        {
            Id = ID;
            Endpoint = localEndpoint;
        }

        public void SetNetworkMode(bool isPublic) => IsPublic = isPublic;
    }
}
