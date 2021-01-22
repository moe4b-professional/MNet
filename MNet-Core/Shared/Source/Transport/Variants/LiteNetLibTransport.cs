using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using LiteNetLib;

namespace MNet
{
    public static partial class NetworkTransportUtility
    {
        public static class LiteNetLib
        {
            public const ushort Port = 9292;

            public static int CheckMTU(DeliveryMode mode)
            {
                switch (mode)
                {
                    case DeliveryMode.Reliable:
                        return int.MaxValue;
                }

                return 400;
            }

            public static class Disconnect
            {
                public static byte[] CodeToBinary(DisconnectCode code)
                {
                    var value = Convert.ToByte(code);

                    return new byte[] { value };
                }

                public static DisconnectCode InfoToCode(DisconnectInfo info)
                {
                    if (info.Reason == DisconnectReason.RemoteConnectionClose || info.Reason == DisconnectReason.ConnectionRejected)
                    {
                        try
                        {
                            var value = info.AdditionalData.GetByte();

                            var code = (DisconnectCode)value;

                            return code;
                        }
                        catch (Exception)
                        {
                            return DisconnectCode.Unknown;
                        }
                    }

                    switch (info.Reason)
                    {
                        case DisconnectReason.DisconnectPeerCalled:
                            return DisconnectCode.Normal;

                        case DisconnectReason.ConnectionFailed:
                            return DisconnectCode.ConnectionFailed;

                        case DisconnectReason.Timeout:
                            return DisconnectCode.Timeout;

                        case DisconnectReason.HostUnreachable:
                            return DisconnectCode.ServerUnreachable;

                        case DisconnectReason.NetworkUnreachable:
                            return DisconnectCode.NetworkUnreachable;

                        case DisconnectReason.ConnectionRejected:
                            return DisconnectCode.Rejected;
                    }

                    return DisconnectCode.Unknown;
                }

                public static DisconnectCode BinaryToCode(byte[] binary) => ByteToCode(binary[0]);
                public static DisconnectCode ByteToCode(byte value) => (DisconnectCode)value;
            }

            public static class Delivery
            {
                public static Glossary<DeliveryMode, DeliveryMethod> Glossary { get; private set; }

                static Delivery()
                {
                    Glossary = new Glossary<DeliveryMode, DeliveryMethod>();

                    Glossary.Add(DeliveryMode.Reliable, DeliveryMethod.ReliableOrdered);
                    Glossary.Add(DeliveryMode.Unreliable, DeliveryMethod.Unreliable);
                }
            }
        }
    }
}