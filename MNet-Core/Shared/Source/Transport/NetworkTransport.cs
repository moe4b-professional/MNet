using System;
using System.Text;
using System.Collections.Generic;
using WebSocketSharp;

namespace MNet
{
    [Preserve]
    public enum NetworkTransportType : byte
    {
        WebSocketSharp, LiteNetLib
    }

    [Preserve]
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

    [Preserve]
    public enum DeliveryMode : byte
    {
        /// <summary>
        /// Reliable and ordered. Packets won't be dropped, won't be duplicated, will arrive in order.
        /// </summary>
        Reliable = 2,

        /// <summary>
        /// Unreliable. Packets can be dropped, can be duplicated, can arrive without order.
        /// </summary>
        Unreliable = 4,
    }

    public static partial class NetworkTransportUtility
    {
        public static partial class Port
        {
            public const ushort Offset = 10240;

            public static ushort From(RoomID id) => From(id.Value);
            public static ushort From(uint value) => (ushort)(value + Offset);
        }

        public static partial class Registeration
        {
            public const byte Success = 200;
        }
    }
}