using System;
using System.Text;
using System.Collections.Generic;

namespace MNet
{
    public enum NetworkTransportType
    {
        WebSocketSharp, LiteNetLib
    }

    public enum DisconnectCode : byte
    {
        Normal, Unknown, FullCapacity, InvalidContext
    }

    public static class NetworkTransportUtility
    {
        public static class WebSocketSharp
        {
            public const ushort DisconnectCodeOffset = 2000;

            public static ushort GetDisconnectValue(DisconnectCode code)
            {
                var value = Convert.ToUInt16(code);

                value += DisconnectCodeOffset;

                return value;
            }

            public static DisconnectCode GetDisconnectCode(ushort value)
            {
                if (value < DisconnectCodeOffset) return DisconnectCode.Unknown;

                value -= DisconnectCodeOffset;

                return (DisconnectCode)value;
            }
        }
    }
}