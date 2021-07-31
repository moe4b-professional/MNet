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

        internal abstract void Invoke<TCommand>(TCommand command) where TCommand : ISyncVarCommand;

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

        internal override void Invoke<TCommand>(TCommand command)
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

        #region Method
        public Packet Sync(T value)
        {
            if (Entity.CheckAuthority(NetworkAPI.Client.Self, authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Set SyncVar '{this}'", Component);
                return default;
            }

            return new Packet(this, value, Behaviour);
        }
        public struct Packet :
            IDeliveryModeConstructor<Packet>,
            IChannelConstructor<Packet>,
            INetworkGroupConstructor<Packet>
        {
            SyncVar<T> Variable { get; }

            T value { get; }

            NetworkEntity.Behaviour Behaviour { get; }
            NetworkEntity Entity => Behaviour.Entity;

            DeliveryMode delivery;
            public Packet Delivery(DeliveryMode value)
            {
                delivery = value;
                return this;
            }

            byte channel;
            public Packet Channel(byte value)
            {
                channel = value;
                return this;
            }

            NetworkGroupID group;
            public Packet Group(NetworkGroupID value)
            {
                group = value;
                return this;
            }

            public void Broadcast()
            {
                var request = BroadcastSyncVarRequest.Write(Entity.ID, Behaviour.ID, Variable.ID, group, value);

                Behaviour.Send(ref request, delivery: delivery, channel: channel);
            }
            public void Buffer()
            {
                var request = BufferSyncVarRequest.Write(Entity.ID, Behaviour.ID, Variable.ID, group, value);

                Behaviour.Send(ref request, delivery: delivery, channel: channel);
            }

            public Packet(SyncVar<T> SyncVar, T value, NetworkEntity.Behaviour behaviour)
            {
                this.Variable = SyncVar;
                this.value = value;
                this.Behaviour = behaviour;

                delivery = DeliveryMode.ReliableOrdered;
                channel = 0;
                group = NetworkGroupID.Default;
            }
        }
        #endregion

        public SyncVar() : this(default) { }
        public SyncVar(T value)
        {
            this.value = value;
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