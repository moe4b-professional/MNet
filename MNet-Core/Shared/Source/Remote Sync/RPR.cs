using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    public struct RprChannelID : IManualNetworkSerializable
    {
        byte value;
        public byte Value { get { return value; } }

        public void Serialize(NetworkWriter writer)
        {
            writer.Insert(value);
        }

        public void Deserialize(NetworkReader reader)
        {
            value = reader.Next();
        }

        public RprChannelID(byte value)
        {
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is RprChannelID target) return Equals(target);

            return false;
        }
        public bool Equals(RprChannelID target) => Equals(value, target.value);

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public static bool operator ==(RprChannelID a, RprChannelID b) => a.Equals(b);
        public static bool operator !=(RprChannelID a, RprChannelID b) => !a.Equals(b);

        public static RprChannelID Increment(RprChannelID channel) => new RprChannelID((byte)(channel.value + 1));
    }

    [Preserve]
    public enum RemoteResponseType : byte
    {
        Success,
        Disconnect,
        InvalidClient,
        InvalidEntity,
        FatalFailure,
    }

    [Preserve]
    public struct RprRequest : INetworkSerializable
    {
        NetworkClientID target;
        public NetworkClientID Target => target;

        RprChannelID channel;
        public RprChannelID Channel => channel;

        RemoteResponseType response;
        public RemoteResponseType Response => response;

        byte[] raw;
        public byte[] Raw => raw;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref target);
            context.Select(ref channel);
            context.Select(ref response);

            switch (response)
            {
                case RemoteResponseType.Success:
                    context.Select(ref raw);
                    break;
            }
        }

        public RprRequest(NetworkClientID target, RprChannelID channel, RemoteResponseType response, byte[] raw)
        {
            this.target = target;
            this.channel = channel;
            this.response = response;
            this.raw = raw;
        }

        public static RprRequest Write(NetworkClientID target, RprChannelID channel, object result, Type type)
        {
            var raw = NetworkSerializer.Serialize(result, type);

            var request = new RprRequest(target, channel, RemoteResponseType.Success, raw);
            return request;
        }

        public static RprRequest Write(NetworkClientID target, RprChannelID channel, RemoteResponseType response)
        {
            var request = new RprRequest(target, channel, response, null);
            return request;
        }
    }

    [Preserve]
    public struct RprResponse : INetworkSerializable
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        RprChannelID channel;
        public RprChannelID Channel => channel;

        RemoteResponseType response;
        public RemoteResponseType Response => response;

        byte[] raw;
        public byte[] Raw => raw;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref sender);
            context.Select(ref channel);
            context.Select(ref response);

            switch (response)
            {
                case RemoteResponseType.Success:
                    context.Select(ref raw);
                    break;
            }
        }

        public RprResponse(NetworkClientID sender, RprChannelID channel, RemoteResponseType response, byte[] raw)
        {
            this.sender = sender;
            this.channel = channel;
            this.response = response;
            this.raw = raw;
        }

        public static RprResponse Write(NetworkClientID sender, RprRequest request)
        {
            var command = new RprResponse(sender, request.Channel, request.Response, request.Raw);
            return command;
        }
    }

    [Preserve]
    public struct RprCommand : INetworkSerializable
    {
        RprChannelID channel;
        public RprChannelID Channel => channel;

        RemoteResponseType response;
        public RemoteResponseType Response => response;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref channel);
            context.Select(ref response);
        }

        public RprCommand(RprChannelID channel, RemoteResponseType response)
        {
            this.channel = channel;
            this.response = response;
        }

        public static RprCommand Write(RprChannelID channel, RemoteResponseType response)
        {
            var request = new RprCommand(channel, response);
            return request;
        }
    }
}