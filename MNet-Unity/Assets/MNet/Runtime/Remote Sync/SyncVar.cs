using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using System.Reflection;

using MB;

namespace MNet
{
    [Serializable]
    public abstract class SyncVar
    {
        [SerializeField]
        protected RemoteAuthority authority = RemoteAuthority.Any;
        public RemoteAuthority Authority => authority;

        public NetworkEntity.Behaviour Behaviour { get; protected set; }

        public SyncVarID ID { get; protected set; }
        public string Name { get; protected set; }

        public abstract Type Argument { get; }

        public Component Component => Behaviour.Component;
        public NetworkEntity Entity => Behaviour.Entity;

        internal void Set(NetworkEntity.Behaviour behaviour, VariableInfo variable, byte index)
        {
            this.Behaviour = behaviour;

            Name = GetName(variable);
            ID = new SyncVarID(index);
        }

        internal abstract void Invoke(ISyncVarCommand command);

        public override string ToString() => $"{Behaviour}->{Name}";

        //Static Utility

        public static SyncVar<T> From<T>(T value, RemoteAuthority authority = RemoteAuthority.Any)
        {
            var syncvar = new SyncVar<T>(value)
            {
                authority = authority,
            };

            return syncvar;
        }

        public static string GetName(VariableInfo variable) => GetName(variable.Member);
        public static string GetName(MemberInfo memeber) => memeber.Name;

        public static bool Is(VariableInfo variable) => Is(variable.ValueType);
        public static bool Is(Type type) => typeof(SyncVar).IsAssignableFrom(type);

        public static SyncVar Assimilate(VariableInfo variable, NetworkEntity.Behaviour behaviour, byte index)
        {
            var value = variable.Read(behaviour.Component) as SyncVar;

            if (value == null)
            {
                value = Activator.CreateInstance(variable.ValueType) as SyncVar;
                variable.Set(behaviour.Component, value);
            }

            value.Set(behaviour, variable, index);

            return value;
        }
    }

    [Serializable]
    public class SyncVar<T> : SyncVar
    {
        [SerializeField]
        T value = default;
        public T Value => value;

        public override Type Argument => typeof(T);

        public delegate void ChangeDelegate(T oldValue, T newValue, SyncVarInfo info);
        public event ChangeDelegate OnChange;
        void Set(T newValue, SyncVarInfo info)
        {
            var oldValue = value;
            value = newValue;

            OnChange?.Invoke(oldValue, newValue, info);
        }

        internal override void Invoke(ISyncVarCommand command)
        {
            T value;
            try
            {
                value = command.Read<T>();
            }
            catch (Exception ex)
            {
                var text = $"Error trying to Parse Value for SyncVar '{this}', Invalid Data Sent Most Likely \n" +
                    $"Exception: \n" +
                    $"{ex}";

                Debug.LogWarning(text, Component);
                return;
            }

            NetworkAPI.Room.Clients.TryGet(command.Sender, out var sender);
            var info = new SyncVarInfo(sender);

            if (Entity.CheckAuthority(info.Sender, authority) == false)
            {
                Debug.LogWarning($"SyncVar '{this}' Command with Invalid Authority Recieved From Client '{command.Sender}'", Component);
                return;
            }

            Set(value, info);
        }

        /// <summary>
        /// Broadcasts SyncVar to All Clients
        /// </summary>
        public BroadcastSyncVarPacket<T> Broadcast(T value)
        {
            if (Entity.CheckAuthority(NetworkAPI.Client.Self, authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Set SyncVar '{this}'", Component);
                return default;
            }

            var packet = new BroadcastSyncVarPacket<T>(this, value, Behaviour);

            return packet;
        }

        /// <summary>
        /// Buffers the SyncVar for all late clients to Recieve
        /// </summary>
        public BufferSyncVarPacket<T> Buffer(T value)
        {
            if (Entity.CheckAuthority(NetworkAPI.Client.Self, authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Set SyncVar '{this}'", Component);
                return default;
            }

            var packet = new BufferSyncVarPacket<T>(this, value, Behaviour);

            return packet;
        }

        public override string ToString() => $"{Component}->{Name}";

        public SyncVar() : this(default) { }
        public SyncVar(T value)
        {
            this.value = value;
        }
    }

    public class SyncVarPacket<TSelf, TValue> :
            FluentObjectRecord.IInterface,
            IDeliveryModeConstructor<TSelf>,
            IChannelConstructor<TSelf>,
            INetworkGroupConstructor<TSelf>
        where TSelf : SyncVarPacket<TSelf, TValue>
    {
        public TSelf self { get; protected set; }

        protected SyncVar<TValue> Variable { get; }

        protected TValue value { get; }

        protected NetworkEntity.Behaviour Behaviour { get; }
        protected NetworkEntity Entity => Behaviour.Entity;

        protected DeliveryMode delivery;
        public TSelf Delivery(DeliveryMode value)
        {
            delivery = value;
            return self;
        }

        protected byte channel;
        public TSelf Channel(byte value)
        {
            channel = value;
            return self;
        }

        protected NetworkGroupID group;
        public TSelf Group(NetworkGroupID value)
        {
            group = value;
            return self;
        }

        public override string ToString()
        {
            return $"{Variable} = {value}";
        }

        SyncVarPacket()
        {
            self = this as TSelf;
        }

        public SyncVarPacket(SyncVar<TValue> SyncVar, TValue value, NetworkEntity.Behaviour behaviour)
        {
            this.Variable = SyncVar;
            this.value = value;
            this.Behaviour = behaviour;

            delivery = DeliveryMode.ReliableOrdered;
            channel = 0;
            group = NetworkGroupID.Default;

            FluentObjectRecord.Add(this);
        }
    }

    public class BroadcastSyncVarPacket<TValue> : SyncVarPacket<BroadcastSyncVarPacket<TValue>, TValue>
    {
        public void Send()
        {
            FluentObjectRecord.Remove(this);

            var request = BroadcastSyncVarRequest.Write(Entity.ID, Behaviour.ID, Variable.ID, group, value);

            Behaviour.Send(ref request, delivery: delivery, channel: channel);
        }

        public BroadcastSyncVarPacket(SyncVar<TValue> SyncVar, TValue value, NetworkEntity.Behaviour behaviour) : base(SyncVar, value, behaviour)
        {

        }
    }

    public class BufferSyncVarPacket<TValue> : SyncVarPacket<BufferSyncVarPacket<TValue>, TValue>
    {
        public void Buffer()
        {
            FluentObjectRecord.Remove(this);

            var request = BufferSyncVarRequest.Write(Entity.ID, Behaviour.ID, Variable.ID, group, value);

            Behaviour.Send(ref request, delivery: delivery, channel: channel);
        }

        public BufferSyncVarPacket(SyncVar<TValue> SyncVar, TValue value, NetworkEntity.Behaviour behaviour) : base(SyncVar, value, behaviour)
        {

        }
    }

    public struct SyncVarInfo
    {
        public NetworkClient Sender { get; private set; }

        /// <summary>
        /// Is this RPC Request the Result of the Room's Buffer
        /// </summary>
        public bool IsBuffered { get; private set; }

        public SyncVarInfo(NetworkClient sender)
        {
            this.Sender = sender;

            this.IsBuffered = NetworkAPI.Realtime.IsOnBuffer;
        }
    }
}