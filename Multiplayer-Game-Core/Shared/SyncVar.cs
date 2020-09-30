using System;
using System.Collections.Generic;
using System.Text;

namespace Backend
{
    public abstract class SyncVarPayload : INetworkSerializable
    {
        protected NetworkEntityID entity;
        public NetworkEntityID Entity => entity;

        protected NetworkBehaviourID behaviour;
        public NetworkBehaviourID Behaviour => behaviour;

        protected string variable;
        public string Variable => variable;

        protected byte[] raw;
        public byte[] Raw => raw;

        public object Read(Type type)
        {
            var value = NetworkSerializer.Deserialize(raw, type);

            return value;
        }

        public virtual void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref entity);
            context.Select(ref behaviour);
            context.Select(ref variable);
            context.Select(ref raw);
        }

        public SyncVarPayload()
        {

        }
    }

    public class SyncVarRequest : SyncVarPayload
    {
        public static SyncVarRequest Write(NetworkEntityID entity, NetworkBehaviourID behaviour, string variable, object value)
        {
            var raw = NetworkSerializer.Serialize(value);

            var request = new SyncVarRequest()
            {
                entity = entity,
                behaviour = behaviour,
                variable = variable,
                raw = raw,
            };

            return request;
        }
    }

    public class SyncVarCommand : SyncVarPayload
    {
        NetworkClientID sender;
        public NetworkClientID Sender => sender;

        public override void Select(INetworkSerializableResolver.Context context)
        {
            base.Select(context);

            context.Select(ref sender);
        }

        public static SyncVarCommand Write(NetworkClientID sender, SyncVarRequest request)
        {
            return Write(sender, request.Entity, request.Behaviour, request.Variable, request.Raw);
        }
        public static SyncVarCommand Write(NetworkClientID sender, NetworkEntityID entity, NetworkBehaviourID behaviour, string variable, byte[] raw)
        {
            var request = new SyncVarCommand()
            {
                sender = sender,
                entity = entity,
                behaviour = behaviour,
                variable = variable,
                raw = raw,
            };

            return request;
        }
    }
}