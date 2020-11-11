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

        /// <summary>
        /// Reliable. Packets won't be dropped, won't be duplicated, can arrive without order.
        /// </summary>
        ReliableUnordered = 0,

        /// <summary>
        /// Reliable only last packet. Packets can be dropped (except the last one), won't
        ///     be duplicated, will arrive in order.
        /// </summary>
        ReliableSequenced = 3,

        /// <summary>
        /// Unreliable. Packets can be dropped, won't be duplicated, will arrive in order.
        /// </summary>
        UnreliableSequenced = 1,
    }

    public static partial class NetworkTransportUtility
    {
        public static partial class Port
        {
            public const ushort Offset = 10240;

            public static ushort From(RoomID id) => From(id.Value);
            public static ushort From(uint value) => (ushort)(value + Offset);
        }
    }
}