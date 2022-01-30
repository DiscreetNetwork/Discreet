using Discreet.Network.Peerbloom.Extensions;
using Discreet.Network.Peerbloom.Protocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom.Protocol.Tcp
{
    //public class TcpProtocol
    //{
    //    IPEndPoint _endpoint;

    //    public TcpProtocol(IPEndPoint endpoint)
    //    {
    //        _endpoint = endpoint;
    //    }

    //    public string GetEndpoint() => $"{_endpoint.Address}:{_endpoint.Port}";
    //    public int GetPort() => _endpoint.Port;

    //    /// <summary>
    //    /// Pings the contact and receives a list of contacts if everything were successful
    //    /// Returns 'null' if the connection could not be established
    //    /// </summary>
    //    /// <returns></returns>
    //    public async Task<List<Contact>> Ping(Contact sender)
    //    {
    //        TcpClient client = new TcpClient();

    //        try
    //        {
    //            await client.ConnectAsync(_endpoint.Address, _endpoint.Port);
    //        }
    //        catch (SocketException e)
    //        {
    //            return null;
    //        }

    //        await client.GetStream().WriteAsync(new PingRequestPacket(sender.Protocol.GetPort()).ToNetworkByteArray());
    //        PingResponsePacket response = new PingResponsePacket(await client.ReadBytesAsync());

    //        return response.Contacts;
    //    }

    //    public async Task Broadcast(string messageId)
    //    {
    //        Console.WriteLine($"Broadcasting: - {messageId} - to - {_endpoint.Address}:{_endpoint.Port}");

    //        TcpClient client = new TcpClient();

    //        try
    //        {
    //            await client.ConnectAsync(_endpoint.Address, _endpoint.Port);
    //        }
    //        catch (SocketException e)
    //        {
    //            Console.WriteLine("Failed to establish connection");
    //        }

    //        WritePacketBase writePacket = new WritePacketBase();
    //        writePacket.WriteString("Broadcast");
    //        writePacket.WriteString(messageId);

    //        try
    //        {
    //            await client.GetStream().WriteAsync(writePacket.ToNetworkByteArray());
    //        }
    //        catch (Exception)
    //        {
    //            throw;
    //        }
    //    }
    //}
}
