using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MNet
{
    public static partial class NetworkTransportUtility
    {
        public static class WebSocket
        {
            public const ushort Port = 9191;

            public static int CheckMTU() => ushort.MaxValue;

            public static class Disconnect
            {
                public const ushort CodeOffset = 4000;

                public static ushort CodeToValue(DisconnectCode code)
                {
                    switch (code)
                    {
                        case DisconnectCode.Normal:
                            return 1000;
                    }

                    var value = Convert.ToUInt16(code);

                    value += CodeOffset;

                    return value;
                }

                public static DisconnectCode ValueToCode(ushort value)
                {
                    if (value < CodeOffset)
                    {
                        switch (value)
                        {
                            case 1000:
                                return DisconnectCode.Normal;
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