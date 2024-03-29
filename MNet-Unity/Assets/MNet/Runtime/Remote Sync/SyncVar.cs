﻿using System;
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

        public static class Parser
        {
            public const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            static Dictionary<Type, VariableInfo[]> Dictionary { get; }

            public static VariableInfo[] Retrieve(Type type)
            {
                if (Dictionary.TryGetValue(type, out var array))
                    return array;

                array = Parse(type);
                Dictionary[type] = array;
                return array;
            }

            static List<VariableInfo> CacheList;
            static HashSet<string> CacheHashSet;
            static Comparison<VariableInfo> Comparer;

            static VariableInfo[] Parse(Type type)
            {
                CacheList.Clear();
                CacheHashSet.Clear();

                var array = type.GetVariables(Flags);

                for (int i = 0; i < array.Count; i++)
                {
                    if (typeof(SyncVar).IsAssignableFrom(array[i].ValueType) == false)
                        continue;

                    var name = GetName(array[i]);

                    if (CacheHashSet.Add(name) == false)
                        throw new Exception($"Multiple '{name}' Sync Vars Registered On '{type}', Please Assign Every Sync Vars a Unique Name And Don't Overload Sync Vars");

                    CacheList.Add(array[i]);
                }

                CacheList.Sort(Comparer);

                if (CacheList.Count > byte.MaxValue)
                    throw new Exception($"NetworkBehaviour {type} Can't Have More than {byte.MaxValue} Sync Vars Defined");

                return CacheList.ToArray();
            }

            static Parser()
            {
                Dictionary = new Dictionary<Type, VariableInfo[]>();
                CacheList = new List<VariableInfo>();
                CacheHashSet = new HashSet<string>();

                Comparer = (VariableInfo a, VariableInfo b) =>
                {
                    return GetName(a).CompareTo(GetName(b));
                };
            }
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
                using (NetworkReader.Pool.Lease(out var stream))
                {
                    stream.Assign(command.Raw);
                    value = stream.Read<T>();
                }
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

    public struct BroadcastSyncVarPacket<TValue> : IDeliveryModeConstructor<BroadcastSyncVarPacket<TValue>>,
            IChannelConstructor<BroadcastSyncVarPacket<TValue>>,
            INetworkGroupConstructor<BroadcastSyncVarPacket<TValue>>
    {
        SyncVar<TValue> Variable;
        TValue value;

        NetworkEntity.Behaviour Behaviour;
        NetworkEntity Entity => Behaviour.Entity;

        DeliveryMode delivery;
        public BroadcastSyncVarPacket<TValue> Delivery(DeliveryMode value)
        {
            delivery = value;
            return this;
        }

        byte channel;
        public BroadcastSyncVarPacket<TValue> Channel(byte value)
        {
            channel = value;
            return this;
        }

        NetworkGroupID group;
        public BroadcastSyncVarPacket<TValue> Group(NetworkGroupID value)
        {
            group = value;
            return this;
        }

        public void Send()
        {
            using (NetworkWriter.Pool.Lease(out var stream))
            {
                stream.Write(value);

                var raw = stream.AsChunk();
                var request = BroadcastSyncVarRequest.Write(Entity.ID, Behaviour.ID, Variable.ID, group, raw);

                Behaviour.Send(ref request, delivery: delivery, channel: channel);
            }
        }

        public BroadcastSyncVarPacket(SyncVar<TValue> SyncVar, TValue value, NetworkEntity.Behaviour behaviour)
        {
            this.Variable = SyncVar;
            this.value = value;
            this.Behaviour = behaviour;

            delivery = DeliveryMode.ReliableOrdered;
            channel = 0;
            group = NetworkGroupID.Default;
        }
    }

    public struct BufferSyncVarPacket<TValue> : IDeliveryModeConstructor<BufferSyncVarPacket<TValue>>,
            IChannelConstructor<BufferSyncVarPacket<TValue>>,
            INetworkGroupConstructor<BufferSyncVarPacket<TValue>>
    {
        SyncVar<TValue> Variable;
        TValue value;

        NetworkEntity.Behaviour Behaviour;
        NetworkEntity Entity => Behaviour.Entity;

        DeliveryMode delivery;
        public BufferSyncVarPacket<TValue> Delivery(DeliveryMode value)
        {
            delivery = value;
            return this;
        }

        byte channel;
        public BufferSyncVarPacket<TValue> Channel(byte value)
        {
            channel = value;
            return this;
        }

        NetworkGroupID group;
        public BufferSyncVarPacket<TValue> Group(NetworkGroupID value)
        {
            group = value;
            return this;
        }

        public void Buffer()
        {
            using (NetworkWriter.Pool.Lease(out var stream))
            {
                stream.Write(value);

                var raw = stream.AsChunk();
                var request = BufferSyncVarRequest.Write(Entity.ID, Behaviour.ID, Variable.ID, group, raw);

                Behaviour.Send(ref request, delivery: delivery, channel: channel);
            }
        }

        public BufferSyncVarPacket(SyncVar<TValue> SyncVar, TValue value, NetworkEntity.Behaviour behaviour)
        {
            this.Variable = SyncVar;
            this.value = value;
            this.Behaviour = behaviour;

            delivery = DeliveryMode.ReliableOrdered;
            channel = 0;
            group = NetworkGroupID.Default;
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

            this.IsBuffered = NetworkAPI.Client.Buffer.IsOn;
        }
    }
}