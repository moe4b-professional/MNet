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

using Cysharp.Threading.Tasks;

using System.Threading;
using MB;

namespace MNet
{
    partial class NetworkEntity
    {
        public partial class Behaviour
        {
            public NetworkEntity Entity { get; protected set; }

            public INetworkBehaviour Contract { get; protected set; }
            public MonoBehaviour Component { get; protected set; }

            public NetworkBehaviourID ID { get; protected set; }

            public CancellationTokenSource DespawnASyncCancellation { get; protected set; }

            #region Events
            public event SetupDelegate OnSetup
            {
                add => Entity.OnSetup += value;
                remove => Entity.OnSetup -= value;
            }

            public event OwnerSetDelegate OnOwnerSet
            {
                add => Entity.OnOwnerSet += value;
                remove => Entity.OnOwnerSet -= value;
            }

            public event SpawnDelegate OnSpawn
            {
                add => Entity.OnSpawn += value;
                remove => Entity.OnSpawn -= value;
            }

            public event ReadyDelegate OnReady
            {
                add => Entity.OnReady += value;
                remove => Entity.OnReady -= value;
            }

            public event DespawnDelegate OnDespawn
            {
                add => Entity.OnDespawn += value;
                remove => Entity.OnDespawn -= value;
            }
            #endregion

            #region RPC
            DualDictionary<RpcID, string, RpcBind> RPCs;

            void ParseRPCs()
            {
                var type = Component.GetType();
                var data = RpcBind.Parser.Retrieve(type);

                for (byte i = 0; i < data.Length; i++)
                {
                    var bind = RpcBind.Retrieve(data[i].Method);
                    bind.Configure(this, data[i].Attribute, data[i].Method, i);

                    RPCs.Add(bind.ID, bind.Name, bind);
                }
            }

            #region Methods
            public BroadcastRpcPacket BroadcastRPC(string method, NetworkWriter stream)
            {
                if (RPCs.TryGetValue(method, out var bind) == false)
                {
                    Debug.LogWarning($"No RPC Found With Name {method} on {Component}", Component);
                    return default;
                }

                if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
                {
                    Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'", Component);
                    return default;
                }

                return new BroadcastRpcPacket(bind, stream, this);
            }
            public struct BroadcastRpcPacket : IDeliveryModeConstructor<BroadcastRpcPacket>,
                IChannelConstructor<BroadcastRpcPacket>,
                INetworkGroupConstructor<BroadcastRpcPacket>,
                IRemoteBufferModeConstructor<BroadcastRpcPacket>,
                INetworkClientExceptionConstructor<BroadcastRpcPacket>
            {
                RpcBind Bind;
                NetworkWriter Stream;

                Behaviour Behaviour;
                NetworkEntity Entity => Behaviour.Entity;

                DeliveryMode delivery;
                public BroadcastRpcPacket Delivery(DeliveryMode value)
                {
                    delivery = value;
                    return this;
                }

                byte channel;
                public BroadcastRpcPacket Channel(byte value)
                {
                    channel = value;
                    return this;
                }

                NetworkGroupID group;
                public BroadcastRpcPacket Group(NetworkGroupID value)
                {
                    group = value;
                    return this;
                }

                RemoteBufferMode buffer;
                public BroadcastRpcPacket Buffer(RemoteBufferMode value)
                {
                    buffer = value;
                    return this;
                }

                NetworkClient exception;
                public BroadcastRpcPacket Exception(NetworkClient value)
                {
                    exception = value;
                    return this;
                }

                public void Send()
                {
                    if (buffer != RemoteBufferMode.None && group != NetworkGroupID.Default)
                        Debug.LogError($"Conflicting Data for RPC Call '{this}', Cannot send to 'None Default' Network Group and Buffer the RPC" +
                            $", This is not Supported, Message will not be Buffered !!!");

                    var chunk = Stream == null ? default(ByteChunk) : Stream.AsChunk();
                    var request = BroadcastRpcRequest.Write(Entity.ID, Behaviour.ID, Bind.ID, buffer, group, exception?.ID, chunk);

                    Behaviour.Send(ref request, delivery: delivery, channel: channel);

                    if (Stream != null) NetworkWriter.Pool.Return(Stream);
                }

                public BroadcastRpcPacket(RpcBind bind, NetworkWriter stream, Behaviour behaviour)
                {
                    this.Bind = bind;
                    this.Stream = stream;
                    this.Behaviour = behaviour;

                    delivery = DeliveryMode.ReliableOrdered;
                    channel = 0;
                    buffer = RemoteBufferMode.Last;
                    group = NetworkGroupID.Default;
                    exception = default;
                }
            }

            public TargetRpcPacket TargetRPC(string method, NetworkClient target, NetworkWriter stream)
            {
                if (RPCs.TryGetValue(method, out var bind) == false)
                {
                    Debug.LogWarning($"No RPC Found With Name {method} on {Component}", Component);
                    return default;
                }

                if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
                {
                    Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'", Component);
                    return default;
                }

                return new TargetRpcPacket(bind, stream, this, target);
            }
            public struct TargetRpcPacket : IDeliveryModeConstructor<TargetRpcPacket>,
                IChannelConstructor<TargetRpcPacket>
            {
                RpcBind Bind;
                NetworkWriter Stream;

                Behaviour Behaviour;
                NetworkEntity Entity => Behaviour.Entity;

                NetworkClient Target { get; }

                DeliveryMode delivery;
                public TargetRpcPacket Delivery(DeliveryMode value)
                {
                    delivery = value;
                    return this;
                }

                byte channel;
                public TargetRpcPacket Channel(byte value)
                {
                    channel = value;
                    return this;
                }

                public void Send()
                {
                    var chunk = Stream == null ? default(ByteChunk) : Stream.AsChunk();
                    var request = TargetRpcRequest.Write(Entity.ID, Behaviour.ID, Bind.ID, Target.ID, chunk);

                    Behaviour.Send(ref request, delivery: delivery, channel: channel);

                    if(Stream != null) NetworkWriter.Pool.Return(Stream);
                }

                public TargetRpcPacket(RpcBind bind, NetworkWriter stream, Behaviour behaviour, NetworkClient target)
                {
                    this.Bind = bind;
                    this.Stream = stream;
                    this.Behaviour = behaviour;
                    this.Target = target;

                    delivery = DeliveryMode.ReliableOrdered;
                    channel = 0;
                }
            }

            public QueryRpcPacket<TResult> QueryRPC<TResult>(string method, NetworkClient target, NetworkWriter stream)
            {
                if (RPCs.TryGetValue(method, out var bind) == false)
                {
                    Debug.LogError($"No RPC With Name '{method}' Found on '{Component}', Component");
                    return default;
                }

                if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
                {
                    Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'", Component);
                    return default;
                }

                return new QueryRpcPacket<TResult>(bind, stream, this, target);
            }
            public struct QueryRpcPacket<TResult> : IDeliveryModeConstructor<QueryRpcPacket<TResult>>,
                IChannelConstructor<QueryRpcPacket<TResult>>
            {
                RpcBind Bind;
                NetworkWriter Stream;

                Behaviour Behaviour;
                NetworkEntity Entity => Behaviour.Entity;

                NetworkClient Target { get; }

                DeliveryMode delivery;
                public QueryRpcPacket<TResult> Delivery(DeliveryMode value)
                {
                    delivery = value;
                    return this;
                }

                byte channel;
                public QueryRpcPacket<TResult> Channel(byte value)
                {
                    channel = value;
                    return this;
                }

                public async UniTask<RprAnswer<TResult>> Send()
                {
                    var promise = NetworkAPI.Client.RPR.Promise(Target);

                    try
                    {
                        var chunk = Stream == null ? default(ByteChunk) : Stream.AsChunk();
                        var request = QueryRpcRequest.Write(Entity.ID, Behaviour.ID, Bind.ID, Target.ID, promise.Channel, chunk);

                        if (Behaviour.Send(ref request, delivery: delivery, channel: channel) == false)
                        {
                            Debug.LogError($"Couldn't Send Query RPC {Bind} to {Target}", Behaviour.Component);
                            return new RprAnswer<TResult>(RemoteResponseType.FatalFailure);
                        }
                    }
                    finally
                    {
                        if(Stream != null) NetworkWriter.Pool.Return(Stream);
                    }

                    await UniTask.WaitUntil(promise.IsComplete);

                    var answer = new RprAnswer<TResult>(promise);

                    return answer;
                }

                public QueryRpcPacket(RpcBind bind, NetworkWriter stream, Behaviour behaviour, NetworkClient target)
                {
                    this.Bind = bind;
                    this.Stream = stream;
                    this.Behaviour = behaviour;
                    this.Target = target;

                    delivery = DeliveryMode.ReliableOrdered;
                    channel = 0;
                }
            }

            public BufferRpcPacket BufferRPC(string method, NetworkWriter stream)
            {
                if (RPCs.TryGetValue(method, out var bind) == false)
                {
                    Debug.LogWarning($"No RPC Found With Name {method} on {Component}", Component);
                    return default;
                }

                if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
                {
                    Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'", Component);
                    return default;
                }

                return new BufferRpcPacket(bind, stream, this);
            }
            public struct BufferRpcPacket : IDeliveryModeConstructor<BufferRpcPacket>,
                IChannelConstructor<BufferRpcPacket>,
                IRemoteBufferModeConstructor<BufferRpcPacket>
            {
                RpcBind Bind;
                NetworkWriter Stream;

                Behaviour Behaviour;
                NetworkEntity Entity => Behaviour.Entity;

                DeliveryMode delivery;
                public BufferRpcPacket Delivery(DeliveryMode value)
                {
                    delivery = value;
                    return this;
                }

                byte channel;
                public BufferRpcPacket Channel(byte value)
                {
                    channel = value;
                    return this;
                }

                RemoteBufferMode buffer;
                public BufferRpcPacket Buffer(RemoteBufferMode value)
                {
                    buffer = value;
                    return this;
                }

                public void Send()
                {
                    var chunk = Stream == null ? default(ByteChunk) : Stream.AsChunk();
                    var request = BufferRpcRequest.Write(Entity.ID, Behaviour.ID, Bind.ID, buffer, chunk);

                    Behaviour.Send(ref request, delivery: delivery, channel: channel);

                    if(Stream != null) NetworkWriter.Pool.Return(Stream);
                }

                public BufferRpcPacket(RpcBind bind, NetworkWriter stream, Behaviour behaviour)
                {
                    this.Bind = bind;
                    this.Stream = stream;
                    this.Behaviour = behaviour;

                    delivery = DeliveryMode.ReliableOrdered;
                    channel = 0;
                    buffer = RemoteBufferMode.Last;
                }
            }
            #endregion

            internal bool InvokeRPC<T>(ref T command)
                where T : struct, IRpcCommand
            {
                if (RPCs.TryGetValue(command.Method, out var bind) == false)
                {
                    Debug.LogError($"Can't Invoke Non-Existant RPC '{Entity}->{Component}->{command.Method}'", Component);
                    return false;
                }

                var info = bind.ParseInfo(command);

                if (Entity.CheckAuthority(info.Sender, bind.Authority) == false)
                {
                    Debug.LogWarning($"RPC Command for '{bind}' with Invalid Authority Recieved From Client '{command.Sender}'", Component);
                    return false;
                }

                var writer = (bind.HasReturn && command.GetType() == typeof(QueryRpcCommand)) ? NetworkWriter.Pool.Take() : default(NetworkWriter);
                try
                {
                    using (NetworkReader.Pool.Lease(out var reader))
                    {
                        reader.Assign(command.Raw);
                        bind.Invoke(reader, writer, info);
                    }
                }
                catch (Exception ex)
                {
                    var text = $"Error trying to Execute RPC ({bind})', Invalid Data Sent Most Likely \n" +
                        $"Exception: \n" +
                        $"{ex}";

                    Debug.LogError(text, Component);
                    return false;
                }

                if (command.Is(out QueryRpcCommand query))
                {
                    NetworkAPI.Client.RPR.Respond(query, writer);
                    NetworkWriter.Pool.Return(writer);
                }

                return true;
            }
            #endregion

            #region SyncVar
            Dictionary<SyncVarID, SyncVar> SyncVars;

            void ParseSyncVars()
            {
                var type = Component.GetType();

                var data = SyncVar.Parser.Retrieve(type);

                for (byte i = 0; i < data.Length; i++)
                {
                    var value = SyncVar.Assimilate(data[i], this, i);

                    SyncVars.Add(value.ID, value);
                }
            }

            internal void InvokeSyncVar<T>(T command)
                where T : ISyncVarCommand
            {
                if (SyncVars.TryGetValue(command.Field, out var variable) == false)
                {
                    Debug.LogWarning($"No SyncVar '{Component}->{command.Field}' Found on to Invoke", Component);
                    return;
                }

                variable.Invoke(command);
            }
            #endregion

            public virtual bool Send<[NetworkSerializationGenerator] T>(ref T payload, DeliveryMode delivery = DeliveryMode.ReliableOrdered, byte channel = 0)
            {
                if (Entity.IsReady == false)
                {
                    Debug.LogError($"Trying to Send Payload '{payload}' from {Component} Before Entity '{Entity}'" +
                        $" is Marked Ready, Please Wait for Ready Or Override {nameof(OnSpawn)}", Component);
                    return false;
                }

                return NetworkAPI.Client.Send(ref payload, mode: delivery, channel: channel);
            }

            void DepsawnCallback()
            {
                DespawnASyncCancellation.Cancel();
                DespawnASyncCancellation.Dispose();
            }

            public override string ToString() => Component.ToString();

            public Behaviour(NetworkEntity entity, INetworkBehaviour contract, NetworkBehaviourID id)
            {
                this.Entity = entity;
                this.Contract = contract;
                Component = contract as MonoBehaviour;
                this.ID = id;

                DespawnASyncCancellation = new CancellationTokenSource();
                OnDespawn += DepsawnCallback;

                RPCs = new DualDictionary<RpcID, string, RpcBind>();
                ParseRPCs();

                SyncVars = new Dictionary<SyncVarID, SyncVar>();
                ParseSyncVars();
            }
        }
    }

    public interface INetworkCallback
    {
        /// <summary>
        /// Callback that allows users to configure Networking callbacks
        /// </summary>
        void OnNetwork();
    }

    public interface INetworkListener : INetworkCallback
    {
        NetworkEntity Entity { get; set; }
    }
    public class NetworkListener : MonoBehaviour, INetworkListener
    {
        public NetworkEntity Entity { get; set; }

        public virtual void OnNetwork() { }
    }

    public interface INetworkBehaviour : INetworkCallback
    {
        NetworkEntity.Behaviour Network { get; set; }
    }
    public class NetworkBehaviour : MonoBehaviour, INetworkBehaviour
    {
        public NetworkEntity.Behaviour Network { get; set; }
        public NetworkEntity Entity => Network.Entity;

        public virtual void OnNetwork() { }
    }
}