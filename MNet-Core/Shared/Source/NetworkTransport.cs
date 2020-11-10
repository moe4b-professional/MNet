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

    public enum DeliveryChannel : byte
    {
        /// <summary>
        /// Reliable and ordered. Packets won't be dropped, won't be duplicated, will arrive in order.
        /// </summary>
        Reliable = 2,
        /// <summary>
        /// Unreliable. Packets can be dropped, can be duplicated, can arrive without order.
        /// </summary>
        Unreliable = 4
    }

    public static partial class NetworkTransportUtility
    {
        
    }
}