using System;
using System.Text;
using System.Collections.Generic;
using WebSocketSharp;

namespace MNet
{
    [Preserve]
    public enum NetworkTransportType : byte
    {
        WebSockets, LiteNetLib
    }

    [Preserve]
    public enum DisconnectCode : byte
    {
        Normal,
        Unknown,

        ConnectionFailed,
        ConnectionClosed,
        ConnectionTimeout,
        ConnectionRejected,

        ServerClosed,
        ServerUnreachable,
        NetworkUnreachable,

        NoCapacity,

        InvalidData,
        InvalidContext,

        HostDisconnected,

        WrongPassword,
    }

    [Preserve]
    public enum DeliveryMode : byte
    {
        //Relaible
        /// <summary>
        /// Reliable and ordered. Packets won't be dropped, won't be duplicated, will arrive in order.
        /// </summary>
        ReliableOrdered,

        /// <summary>
        /// Reliable. Packets won't be dropped, won't be duplicated, can arrive without order.
        /// </summary>
        ReliableUnordered,

        /// <summary>
        /// Reliable only last packet. Packets can be dropped (except the last one), won't be duplicated, will arrive in order.
        /// </summary>
        ReliableSequenced,

        //Unreliable
        /// <summary>
        /// Unreliable. Packets can be dropped, can be duplicated, can arrive without order.
        /// </summary>
        Unreliable,

        /// <summary>
        /// Unreliable. Packets can be dropped, won't be duplicated, will arrive in order.
        /// </summary>
        UnreliableSequenced,
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