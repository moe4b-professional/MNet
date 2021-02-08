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
                    case DeliveryMode.ReliableOrdered:
                    case DeliveryMode.ReliableUnordered:
                        return ushort.MaxValue;
                }

                return 1024;
            }

            public static class Disconnect
            {
                public static DisconnectCode InfoToCode(DisconnectInfo info)
                {
                    if (info.Reason == DisconnectReason.RemoteConnectionClose || info.Reason == DisconnectReason.ConnectionRejected)
                    {
                        if(info.AdditionalData.TryGetByte(out var value))
                            return BinaryToCode(value);

                        return DisconnectCode.Unknown;
                    }

                    switch (info.Reason)
                    {
                        case DisconnectReason.DisconnectPeerCalled:
                            return DisconnectCode.Normal;

                        case DisconnectReason.ConnectionFailed:
                            return DisconnectCode.ConnectionFailed;

                        case DisconnectReason.Timeout:
                            return DisconnectCode.ConnectionTimeout;

                        case DisconnectReason.HostUnreachable:
                            return DisconnectCode.ServerUnreachable;

                        case DisconnectReason.NetworkUnreachable:
                            return DisconnectCode.NetworkUnreachable;

                        case DisconnectReason.ConnectionRejected:
                            return DisconnectCode.ConnectionRejected;

                        case DisconnectReason.RemoteConnectionClose:
                            return DisconnectCode.ConnectionClosed;
                    }

                    return DisconnectCode.Unknown;
                }

                public static byte[] CodeToBinary(DisconnectCode code)
                {
                    var value = Convert.ToByte(code);

                    return new byte[] { value };
                }

                public static DisconnectCode BinaryToCode(byte value) => (DisconnectCode)value;
            }

            public static class Delivery
            {
                public static readonly Glossary<DeliveryMode, DeliveryMethod> Glossary;

                static Delivery()
                {
                    Glossary = new Glossary<DeliveryMode, DeliveryMethod>();

                    Glossary.Add(DeliveryMode.ReliableOrdered, DeliveryMethod.ReliableOrdered);
                    Glossary.Add(DeliveryMode.ReliableUnordered, DeliveryMethod.ReliableUnordered);
                    Glossary.Add(DeliveryMode.ReliableSequenced, DeliveryMethod.ReliableSequenced);

                    Glossary.Add(DeliveryMode.Unreliable, DeliveryMethod.Unreliable);
                    Glossary.Add(DeliveryMode.UnreliableSequenced, DeliveryMethod.Sequenced);
                }
            }
        }
    }
}