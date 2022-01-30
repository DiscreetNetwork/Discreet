using Discreet.Network.Peerbloom.Protocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom.Protocol.Tcp
{
    //public class PingResponsePacket
    //{
    //    public List<Contact> Contacts;

    //    /// <summary>
    //    /// Used when sending the response inside the 'Receiver'
    //    /// </summary>
    //    public PingResponsePacket(List<Contact> contacts)
    //    {
    //        Contacts = contacts;
    //    }

    //    /// <summary>
    //    /// Used when receiving the response inside the 'Protocol'
    //    /// </summary>
    //    /// <param name="bytes"></param>
    //    public PingResponsePacket(byte[] bytes)
    //    {
    //        Contacts = new List<Contact>();

    //        ReadPacketBase readPacket = new ReadPacketBase(bytes);

    //        int contactListLength = readPacket.ReadInt();
    //        for (int i = 0; i < contactListLength; i++)
    //        {
    //            IPEndPoint endpoint = IPEndPoint.Parse(readPacket.ReadString());
    //            Contacts.Add(new Contact(new TcpProtocol(endpoint)));
    //        }
    //    }

    //    /// <summary>
    //    /// Used to convert the POCO into a byte array that is ready to be sent across the network
    //    /// </summary>
    //    /// <returns></returns>
    //    public byte[] ToNetworkByteArray()
    //    {
    //        WritePacketBase packet = new WritePacketBase();

    //        packet.WriteInt(Contacts.Count());
    //        foreach (var contact in Contacts)
    //        {
    //            packet.WriteString(contact.Protocol.GetEndpoint());
    //        }

    //        return packet.ToNetworkByteArray();
    //    }
    //}
}
