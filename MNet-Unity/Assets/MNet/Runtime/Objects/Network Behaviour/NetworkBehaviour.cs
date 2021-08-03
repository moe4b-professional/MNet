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
                var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

                var type = Component.GetType();

                var methods = type.GetMethods(flags).Where(NetworkRPCAttribute.Defined).OrderBy(RpcBind.GetName).ToArray();

                if (methods.Length > byte.MaxValue)
                    throw new Exception($"NetworkBehaviour {GetType().Name} Can't Have More than {byte.MaxValue} RPCs Defined");

                for (byte i = 0; i < methods.Length; i++)
                {
                    var attribute = NetworkRPCAttribute.Retrieve(methods[i]);

                    var bind = new RpcBind(this, attribute, methods[i], i);

                    if (RPCs.Contains(bind.Name))
                        throw new Exception($"Rpc '{bind.Name}' Already Registered On '{GetType()}', Please Assign Every RPC a Unique Name And Don't Overload RPC Methods");

                    RPCs.Add(bind.ID, bind.Name, bind);
                }
            }

            #region Methods
            public class RpcPacket<TSelf> : FluentObjectRecord.IInterface,
                IDeliveryModeConstructor<TSelf>,
                IChannelConstructor<TSelf>
                where TSelf : RpcPacket<TSelf>
            {
                public TSelf self { get; protected set; }

                protected RpcBind Bind { get; }

                protected object[] Arguments { get; }

                protected Behaviour Behaviour { get; }
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

                protected bool Send<T>(ref T payload)
                {
                    FluentObjectRecord.Remove(this);

                    return Behaviour.Send(ref payload, delivery: delivery, channel: channel);
                }

                public override string ToString()
                {
                    return $"{Bind}{Arguments.ToCollectionString()}";
                }

                public RpcPacket(RpcBind bind, object[] arguments, Behaviour behaviour)
                {
                    this.Bind = bind;
                    this.Arguments = arguments;
                    this.Behaviour = behaviour;

                    self = this as TSelf;

                    FluentObjectRecord.Add(this);
                }
            }

            public BroadcastRpcPacket BroadcastRPC(string method, params object[] arguments)
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

                return new BroadcastRpcPacket(bind, arguments, this);
            }
            public class BroadcastRpcPacket : RpcPacket<BroadcastRpcPacket>,
                INetworkGroupConstructor<BroadcastRpcPacket>,
                IRemoteBufferModeConstructor<BroadcastRpcPacket>,
                INetworkClientExceptionConstructor<BroadcastRpcPacket>
            {
                NetworkGroupID group = NetworkGroupID.Default;
                public BroadcastRpcPacket Group(NetworkGroupID value)
                {
                    group = value;
                    return this;
                }

                RemoteBufferMode buffer = RemoteBufferMode.None;
                public BroadcastRpcPacket Buffer(RemoteBufferMode value)
                {
                    buffer = value;
                    return this;
                }

                NetworkClient exception = null;
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

                    var raw = Bind.SerializeArguments(Arguments);
                    var request = BroadcastRpcRequest.Write(Entity.ID, Behaviour.ID, Bind.ID, buffer, group, exception?.ID, raw);

                    Send(ref request);
                }

                public BroadcastRpcPacket(RpcBind bind, object[] arguments, Behaviour behaviour)
                    : base(bind, arguments, behaviour)
                {

                }
            }

            public TargetRpcPacket TargetRPC(string method, NetworkClient target, params object[] arguments)
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

                return new TargetRpcPacket(bind, arguments, this, target);
            }
            public class TargetRpcPacket : RpcPacket<TargetRpcPacket>
            {
                NetworkClient Target { get; }

                public void Send()
                {
                    var raw = Bind.SerializeArguments(Arguments);
                    var request = TargetRpcRequest.Write(Entity.ID, Behaviour.ID, Bind.ID, Target.ID, raw);

                    Send(ref request);
                }

                public TargetRpcPacket(RpcBind bind, object[] arguments, Behaviour behaviour, NetworkClient target)
                    : base(bind, arguments, behaviour)
                {
                    this.Target = target;
                }
            }

            public QueryRpcPacket<TResult> QueryRPC<TResult>(string method, NetworkClient target, params object[] arguments)
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

                return new QueryRpcPacket<TResult>(bind, arguments, this, target);
            }
            public class QueryRpcPacket<TResult> : RpcPacket<QueryRpcPacket<TResult>>
            {
                NetworkClient Target { get; }

                public async UniTask<RprAnswer<TResult>> Send()
                {
                    var promise = NetworkAPI.Client.RPR.Promise(Target);

                    var raw = Bind.SerializeArguments(Arguments);
                    var request = QueryRpcRequest.Write(Entity.ID, Behaviour.ID, Bind.ID, Target.ID, promise.Channel, raw);

                    if (Send(ref request) == false)
                    {
                        Debug.LogError($"Couldn't Send Query RPC {Bind} to {Target}", Behaviour.Component);
                        return new RprAnswer<TResult>(RemoteResponseType.FatalFailure);
                    }

                    await UniTask.WaitUntil(promise.IsComplete);

                    var answer = new RprAnswer<TResult>(promise);

                    return answer;
                }

                public QueryRpcPacket(RpcBind bind, object[] arguments, Behaviour behaviour, NetworkClient target)
                    : base(bind, arguments, behaviour)
                {
                    this.Target = target;
                }
            }

            public BufferRpcPacket BufferRPC(string method, params object[] arguments)
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

                return new BufferRpcPacket(bind, arguments, this);
            }
            public class BufferRpcPacket : RpcPacket<BufferRpcPacket>,
                IRemoteBufferModeConstructor<BufferRpcPacket>
            {
                RemoteBufferMode buffer = RemoteBufferMode.Last;
                public BufferRpcPacket Buffer(RemoteBufferMode value)
                {
                    buffer = value;
                    return this;
                }

                public void Send()
                {
                    var raw = Bind.SerializeArguments(Arguments);
                    var request = BufferRpcRequest.Write(Entity.ID, Behaviour.ID, Bind.ID, buffer, raw);

                    Send(ref request);
                }

                public BufferRpcPacket(RpcBind bind, object[] arguments, NetworkEntity.Behaviour behaviour)
                    : base(bind, arguments, behaviour)
                {

                }
            }
            #endregion

            #region Invoke
            internal bool InvokeRPC<T>(ref T command)
                where T : IRpcCommand
            {
                if (RPCs.TryGetValue(command.Method, out var bind) == false)
                {
                    Debug.LogError($"Can't Invoke Non-Existant RPC '{Entity}->{Component}->{command.Method}'", Component);
                    return false;
                }

                object[] arguments;
                RpcInfo info;
                try
                {
                    bind.ParseCommand(command, out arguments, out info);
                }
                catch (Exception ex)
                {
                    var text = $"Error trying to Parse RPC Arguments of {bind}', Invalid Data Sent Most Likely \n" +
                        $"Exception: \n" +
                        $"{ex}";

                    Debug.LogError(text, Component);
                    return false;
                }

                if (Entity.CheckAuthority(info.Sender, bind.Authority) == false)
                {
                    Debug.LogWarning($"RPC Command for '{bind}' with Invalid Authority Recieved From Client '{command.Sender}'", Component);
                    return false;
                }

                object result;
                try
                {
                    result = bind.Invoke(arguments);
                }
                catch (TargetInvocationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var text = $"Error Trying to Invoke RPC {bind}', " +
                        $"Please Ensure Method is Implemented And Invoked Correctly\n" +
                        $"Exception: \n" +
                        $"{ex}";

                    Debug.LogError(text, Component);

                    return false;
                }

                if (bind.IsCoroutine) ExceuteCoroutineRPC(result as IEnumerator);

                if (command is QueryRpcCommand query)
                {
                    if (bind.IsAsync)
                        AwaitAsyncQueryRPC(result as IUniTask, query, bind).Forget();
                    else
                        NetworkAPI.Client.RPR.Respond(query, result, bind.ReturnType);
                }

                return true;
            }

            void ExceuteCoroutineRPC(IEnumerator method) => Component.StartCoroutine(method);

            async UniTask AwaitAsyncQueryRPC(IUniTask task, QueryRpcCommand command, RpcBind bind)
            {
                while (task.Status == UniTaskStatus.Pending) await UniTask.Yield();

                if (NetworkAPI.Client.IsConnected == false)
                {
                    //Debug.LogWarning($"Will not Respond to Async Query RPC {bind} Because Client Disconnected, The Server Will Provide a Default Response to the Requester");
                    return;
                }

                if (task.Status != UniTaskStatus.Succeeded)
                {
                    NetworkAPI.Client.RPR.Respond(command, RemoteResponseType.FatalFailure);
                    return;
                }

                NetworkAPI.Client.RPR.Respond(command, task.Result, task.Type);
            }
            #endregion

            #endregion

            #region SyncVar
            Dictionary<SyncVarID, SyncVar> SyncVars;

            void ParseSyncVars()
            {
                var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

                var type = Component.GetType();

                var variables = type.GetVariables(flags).Where(SyncVar.Is).OrderBy(SyncVar.GetName).ToArray();

                if (variables.Length > byte.MaxValue)
                    throw new Exception($"NetworkBehaviour {GetType().Name} Can't Have More than {byte.MaxValue} SyncVars Defined");

                for (byte i = 0; i < variables.Length; i++)
                {
                    var value = SyncVar.Assimilate(variables[i], this, i);

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

            public virtual bool Send<T>(ref T payload, DeliveryMode delivery = DeliveryMode.ReliableOrdered, byte channel = 0)
            {
                if (Entity.IsReady == false)
                {
                    Debug.LogError($"Trying to Send Payload '{payload}' from {Component} Before Entity '{Entity}' is Marked Ready, Please Wait for Ready Or Override {nameof(OnSpawn)}", Component);
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

    public interface INetworkBehaviour
    {
        NetworkEntity.Behaviour Network { get; set; }

        void OnNetwork();
    }

    public class NetworkBehaviour : MonoBehaviour, INetworkBehaviour
    {
        public NetworkEntity.Behaviour Network { get; set; }
        public NetworkEntity Entity => Network.Entity;

        public virtual void OnNetwork() { }
    }
}