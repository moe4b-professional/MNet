using System;
using System.Text;
using System.Collections.Generic;
using WebSocketSharp;

namespace MNet
{
    public enum NetworkTransportType
    {
        WebSocketSharp, LiteNetLib
    }

    public enum DisconnectCode : byte
    {
        Normal,
        Unknown,
        Rejected,
        NetworkUnreachable,
        ServerUnreachable,
        ConnectionFailed,
        Timeout,
        FullCapacity,
        InvalidContext,
        InvalidData
    }
}