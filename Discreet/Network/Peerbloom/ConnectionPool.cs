using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public class ConnectionPool
    {
        List<RemoteNode> _outBoundConnections = new List<RemoteNode>();
        List<RemoteNode> _inboundConnections = new List<RemoteNode>();

        public void AddOutboundConnection(RemoteNode node)
        {
            if (_outBoundConnections.Any(n => n.Id.Value == node.Id.Value)) return;

            _outBoundConnections.Add(node);
        }

        public List<RemoteNode> GetOutboundConnections() => _outBoundConnections.ToList();

        public void AddInboundConnection(RemoteNode node)
        {
            if (_inboundConnections.Any(n => n.Id.Value == node.Id.Value)) return;

            _inboundConnections.Add(node);
        }

        public RemoteNode FindNodeInPool(IPEndPoint endpoint)
        {
            foreach (var node in _outBoundConnections)
            {
                if (node.Endpoint.Address.MapToIPv4().Equals(endpoint.Address.MapToIPv4()))
                {
                    return node;
                }
            }

            foreach (var node in _inboundConnections)
            {
                if (node.Endpoint.Address.MapToIPv4().Equals(endpoint.Address.MapToIPv4()))
                {
                    return node;
                }
            }

            return null;
        }

        public void RemoveNodeFromPool(RemoteNode node)
        {
            foreach (var _node in _inboundConnections)
            {
                if (node == _node)
                {
                    _inboundConnections.Remove(node);
                }
            }

            foreach (var _node in _outBoundConnections)
            {
                if (node == _node)
                {
                    _outBoundConnections.Remove(node);
                }
            }
        }

        public List<RemoteNode> GetInboundConnections() => _inboundConnections.ToList();
    }
}
