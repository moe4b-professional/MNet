using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using WebSocketSharp;

namespace MNet
{
    public static partial class NetworkTransportUtility
    {
        public static class WebSocket
        {
            public const ushort Port = 9191;

            public static int CheckMTU(DeliveryMode mode) => int.MaxValue;

            public static class Disconnect
            {
                public const ushort CodeOffset = 2000;

                public static ushort CodeToValue(DisconnectCode code)
                {
                    var value = Convert.ToUInt16(code);

                    value += CodeOffset;

                    return value;
                }

                public static DisconnectCode ValueToCode(ushort value)
                {
                    if (value < CodeOffset)
                    {
                        var code = (CloseStatusCode)value;

                        switch (code)
                        {
                            case CloseStatusCode.Normal:
                                return DisconnectCode.Normal;

                            case CloseStatusCode.InvalidData:
                                return DisconnectCode.InvalidData;
                        }

                        return DisconnectCode.Unknown;
                    }
                    else
                    {
                        value -= CodeOffset;
                        return (DisconnectCode)value;
                    }
                }
            }
        }
    }
}