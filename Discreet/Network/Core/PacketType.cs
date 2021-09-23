using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Network.Core
{
    [Flags]
    public enum MessageType : byte
    {
        Ping = 0x01,
        Ack = 0x00,
        RequestPing = 0x05,
        RequestAck = 0x04,
        ForwardedPing = 0x07,
        ForwardedAck = 0x06,
    }
}
