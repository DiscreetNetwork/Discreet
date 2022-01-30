using Discreet.Network.Peerbloom.Extensions;
using Discreet.Network.Peerbloom.Protocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom.Protocol.Tcp
{
    //public class TcpReceiver : IReceiver
    //{
    //    public LocalNode Node { get; set; }
    //    List<string> _receivedPacketIds = new List<string>();

    //    private readonly int _port;
    //    private readonly CancellationToken _cancellationToken;
    //    TcpListener _listener;


    //    public TcpReceiver(LocalNode node, int port, CancellationToken cancellationToken)
    //    {
    //        Node = node;
    //        _port = port;
    //        _cancellationToken = cancellationToken;
    //    }

    //    public async Task Listen()
    //    {
    //        _listener = new TcpListener(IPAddress.Any, _port);
    //        _listener.Start();
    //        while(!_cancellationToken.IsCancellationRequested)
    //        {
    //            TcpClient client = await _listener.AcceptTcpClientAsync();
    //            _ = HandleClient(client);
    //        }
    //    }

    //    async Task HandleClient(TcpClient client)
    //    {
    //        NetworkRequestPacket networkRequest = new NetworkRequestPacket(await client.ReadBytesAsync());
    //        switch(networkRequest.PacketType)
    //        {
    //            case "PingRequestPacket":
    //                await OnPingRequestReceived(client, networkRequest.ConvertTo<PingRequestPacket>());
    //                break;

    //            case "Broadcast":
    //                await OnBroadcastReceived(networkRequest.ConvertTo<ReadPacketBase>());
    //                break;
    //        }

    //        client.Dispose();
    //    }


    //    async Task OnPingRequestReceived(TcpClient client, PingRequestPacket request)
    //    {
    //        IPEndPoint senderEndpoint = ((IPEndPoint)client.Client.RemoteEndPoint);
    //        senderEndpoint.Port = request.Port;

    //        Console.WriteLine($"Received ping from: {senderEndpoint}");

    //        // Take 25% of the peers and send them back
    //        var peers = Node.Peers.GetContacts().Where(x => !x.Protocol.GetEndpoint().Equals($"{senderEndpoint.Address}:{senderEndpoint.Port}")).ToList();
    //        peers = peers.TakeRandomPercent(1, 4);

    //        Contact senderContact = new Contact(new TcpProtocol(senderEndpoint));
    //        Node.AddContact(senderContact);

    //        PingResponsePacket response = new PingResponsePacket(peers);
    //        await client.GetStream().WriteAsync(response.ToNetworkByteArray());
    //    }


    //    /// <summary>
    //    /// TODO: TTL - Research packet forwarding (how many times should it be forwarded?)
    //    /// </summary>
    //    /// <param name="packet"></param>
    //    /// <returns></returns>
    //    async Task OnBroadcastReceived(ReadPacketBase packet)
    //    {
    //        string messageId = packet.ReadString();
    //        if (Node.MessageStore.Contains(messageId))
    //        {
    //            Console.WriteLine($"Received duplicate broadcast: {messageId} - No action");
    //            return;
    //        }

    //        Node.MessageStore.AddMessageIdentifier(messageId);

    //        Console.WriteLine($"Received broadcast: {messageId} - Rebroadcasting action");
    //        var peers = Node.Peers.GetContacts();

    //        foreach (var peer in peers)
    //        {
    //            // We dont await this, we just fire and forget
    //            _ = peer.Protocol.Broadcast(messageId);
    //        }
    //    }
    //}
}
