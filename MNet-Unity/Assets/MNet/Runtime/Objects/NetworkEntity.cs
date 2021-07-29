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
using System.Threading;
using System.Reflection;
using Cysharp.Threading.Tasks;

using MB;

namespace MNet
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder)]
    [AddComponentMenu(Constants.Path + "Network Entity")]
    public partial class NetworkEntity : MonoBehaviour
    {
        public const int ExecutionOrder = -500;

        [SerializeField]
        SyncProperty sync = default;
        public SyncProperty Sync => sync;
        [Serializable]
        public class SyncProperty
        {
            [SerializeField]
            [SyncInterval(0, 200)]
            [Tooltip("Sync Interval in ms, 1s = 1000ms")]
            int interval = 100;
            public int Interval => interval;

            public List<ISync> List { get; protected set; }

            public event Action OnInvoke;

            public bool Active => List.Count > 0;

            NetworkEntity entity;
            internal void Set(NetworkEntity reference)
            {
                entity = reference;

                List = entity.GetComponentsInChildren<ISync>(true).ToList();

                entity.OnReady += ReadyCallback;
                entity.OnDespawn += DespawnCallback;
            }

            void ReadyCallback()
            {
                if (Active) coroutine = entity.StartCoroutine(Procedure());
            }

            Coroutine coroutine;
            IEnumerator Procedure()
            {
                while (true)
                {
                    if (entity.IsMine) Invoke();

                    yield return new WaitForSecondsRealtime(interval / 1000f);
                }
            }

            void Invoke()
            {
                for (int i = 0; i < List.Count; i++)
                    List[i].Sync();

                OnInvoke?.Invoke();
            }

            void DespawnCallback()
            {
                if (coroutine != null)
                {
                    entity.StopCoroutine(coroutine);
                    coroutine = null;
                }
            }
        }

        public interface ISync
        {
            void Sync();
        }

        public NetworkEntityID ID { get; protected set; }

        public bool IsConnected => NetworkAPI.Client.IsConnected;

        /// <summary>
        /// Boolean value to show if this Entity is ready to send and recieve messages
        /// </summary>
        public bool IsReady { get; protected set; } = false;

        #region Type
        public EntityType Type { get; internal set; }

        public bool IsSceneObject => Type == EntityType.SceneObject;
        public bool IsDynamic => Type == EntityType.Dynamic;
        public bool IsOrphan => Type == EntityType.Orphan;

        /// <summary>
        /// Is this an entity that will always be owned by the master client? such as scene objects and orphans
        /// </summary>
        public bool IsMasterObject => CheckIfMasterObject(Type);
        #endregion

        public PersistanceFlags Persistance { get; protected set; }

        #region Ownership
        public NetworkClient Owner { get; protected set; }

        /// <summary>
        /// Am I the owner of this entity?
        /// </summary>
        public bool IsMine => Owner == NetworkAPI.Client.Self;

        public delegate void OwnerSetDelegate(NetworkClient client);
        public event OwnerSetDelegate OnOwnerSet;
        internal void SetOwner(NetworkClient client)
        {
            Owner = client;

            OnOwnerSet?.Invoke(Owner);
        }

        public bool Takeover()
        {
            if (IsMasterObject)
            {
                Log.Error($"Master Objects Cannot be Taken Over by Clients");
                return false;
            }

            if (IsMine)
            {
                Debug.LogWarning($"You Already Own Entity {this}, no Need to Takeover it");
                return false;
            }

            var request = new TakeoverEntityRequest(ID);

            return NetworkAPI.Client.Send(ref request);
        }

        public bool TransferTo(NetworkClient client)
        {
            if (IsMasterObject)
            {
                Log.Error($"Master Objects Cannot be Transfered");
                return false;
            }

            if (Owner == client)
            {
                Debug.LogWarning($"Client: {client} Already Owns Entity '{this}'");
                return false;
            }

            if (CheckAuthority(NetworkAPI.Client.Self) == false)
            {
                Debug.LogWarning($"Local Client has no Authority over Entity '{this}', Entity Trasnsfer Invalid");
                return false;
            }

            var payload = new TransferEntityPayload(ID, client.ID);

            return NetworkAPI.Client.Send(ref payload);
        }

        /// <summary>
        /// Checks if client has authority over this Entity
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckAuthority(NetworkClient client) => CheckAuthority(client, RemoteAuthority.Owner | RemoteAuthority.Master);
        /// <summary>
        /// Checks if client has authority over this Entity with RemoteAuthority flags
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckAuthority(NetworkClient client, RemoteAuthority authority)
        {
            //instantly validate every buffered message
            if (NetworkAPI.Realtime.IsOnBuffer) return true;

            if (authority.HasFlag(RemoteAuthority.Any)) return true;

            if (client == null) return false;

            if (authority.HasFlag(RemoteAuthority.Owner))
            {
                if (client == Owner)
                    return true;
            }

            if (authority.HasFlag(RemoteAuthority.Master))
            {
                if (client == NetworkAPI.Room.Master.Client)
                    return true;
            }

            return false;
        }
        #endregion

        public AttributesCollection Attributes { get; protected set; }

        public DualDictionary<NetworkBehaviourID, MonoBehaviour, Behaviour> Behaviours { get; protected set; }

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

                    RPCs.Add(bind.MethodID, bind.Name, bind);
                }
            }

            #region Methods
            public bool BroadcastRPC(string method, RemoteBufferMode buffer, DeliveryMode delivery, byte channel, NetworkGroupID group, NetworkClient exception, params object[] arguments)
            {
                if (RPCs.TryGetValue(method, out var bind) == false)
                {
                    Debug.LogWarning($"No RPC Found With Name {method} on {Component}", Component);
                    return false;
                }

                if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
                {
                    Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'", Component);
                    return false;
                }

                var raw = bind.WriteArguments(arguments);

                var request = BroadcastRpcRequest.Write(Entity.ID, ID, bind.MethodID, buffer, group, exception?.ID, raw);

                return Send(ref request, delivery, channel);
            }

            public bool TargetRPC(string method, NetworkClient target, DeliveryMode delivery, byte channel, params object[] arguments)
            {
                if (RPCs.TryGetValue(method, out var bind) == false)
                {
                    Debug.LogWarning($"No RPC Found With Name {method} on {Component}", Component);
                    return false;
                }

                if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
                {
                    Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'", Component);
                    return false;
                }

                var raw = bind.WriteArguments(arguments);

                var request = TargetRpcRequest.Write(Entity.ID, ID, bind.MethodID, target.ID, raw);

                return Send(ref request, delivery, channel);
            }

            public async UniTask<RprAnswer<TResult>> QueryRPC<TResult>(string method, NetworkClient target, DeliveryMode delivery, byte channel, params object[] arguments)
            {
                if (RPCs.TryGetValue(method, out var bind) == false)
                {
                    Debug.LogError($"No RPC With Name '{method}' Found on '{Component}', Component");
                    return new RprAnswer<TResult>(RemoteResponseType.FatalFailure);
                }

                if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
                {
                    Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'", Component);
                    return new RprAnswer<TResult>(RemoteResponseType.FatalFailure);
                }

                var promise = NetworkAPI.Client.RPR.Promise(target);

                var raw = bind.WriteArguments(arguments);

                var request = QueryRpcRequest.Write(Entity.ID, ID, bind.MethodID, target.ID, promise.Channel, raw);

                if (Send(ref request, delivery, channel) == false)
                {
                    Debug.LogError($"Couldn't Send Query RPC {method} to {target}", Component);
                    return new RprAnswer<TResult>(RemoteResponseType.FatalFailure);
                }

                await UniTask.WaitUntil(promise.IsComplete);

                var answer = new RprAnswer<TResult>(promise);

                return answer;
            }

            public bool BufferRPC(string method, RemoteBufferMode buffer, DeliveryMode delivery, byte channel, params object[] arguments)
            {
                if (RPCs.TryGetValue(method, out var bind) == false)
                {
                    Debug.LogWarning($"No RPC Found With Name {method} on {Component}", Component);
                    return false;
                }

                if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
                {
                    Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'", Component);
                    return false;
                }

                var raw = bind.WriteArguments(arguments);

                var request = BufferRpcRequest.Write(Entity.ID, ID, bind.MethodID, buffer, raw);

                return Send(ref request, delivery, channel);
            }
            #endregion

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
                catch (TargetInvocationException ex)
                {
                    throw ex;
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

        public virtual Behaviour GetBehaviour<T>(T reference)
            where T : MonoBehaviour
        {
            if (Behaviours.TryGetValue(reference, out var behaviour) == false)
                throw new Exception($"Cannot Retrieve Network Behaviour for Unregisterd Type of {typeof(T).Name}");

            return behaviour;
        }

        public Scene UnityScene => gameObject.scene;
        public NetworkScene NetworkScene { get; protected set; }

        protected virtual void Awake()
        {
            if (Application.isPlaying == false)
            {
                NetworkScene.RegisterLocal(this);
                return;
            }

            Behaviours = new DualDictionary<NetworkBehaviourID, MonoBehaviour, Behaviour>();

            var targets = GetComponentsInChildren<INetworkBehaviour>(true);

            if (targets.Length > byte.MaxValue)
                throw new Exception($"Entity {name} May Only Have Up To {byte.MaxValue} Behaviours, Current Count: {targets.Length}");

            for (byte i = 0; i < targets.Length; i++)
            {
                var id = new NetworkBehaviourID(i);

                var behaviour = new Behaviour(this, targets[i], id);

                Behaviours[id, behaviour.Component] = behaviour;

                targets[i].Network = behaviour;
                targets[i].OnNetwork();
            }
        }

        public delegate void SetupDelegate();
        public event SetupDelegate OnSetup;
        internal void Setup(NetworkClient owner, EntityType type, PersistanceFlags persistance, AttributesCollection attributes)
        {
            this.Type = type;
            this.Persistance = persistance;
            this.Attributes = attributes;
            this.Owner = owner;

            sync.Set(this);

            OnSetup?.Invoke();

            SetOwner(owner);
        }

        public delegate void SpawnDelegate();
        public event SpawnDelegate OnSpawn;
        internal void Spawn(NetworkEntityID id, NetworkScene scene)
        {
            this.ID = id;
            this.NetworkScene = scene;

            if (IsDynamic || IsOrphan) name += $" {id}";

            IsReady = true;

            OnSpawn?.Invoke();

            if (NetworkAPI.Realtime.IsOnBuffer)
                ReadyAfterBuffer();
            else
                Ready();
        }

        #region Ready After Buffer
        void ReadyAfterBuffer()
        {
            NetworkAPI.Realtime.OnBufferEnd += ReadyAfterBufferCallback;
        }

        void ReadyAfterBufferCallback(IList<NetworkMessage> list)
        {
            NetworkAPI.Realtime.OnBufferEnd -= ReadyAfterBufferCallback;
            Ready();
        }
        #endregion

        public delegate void ReadyDelegate();
        public event ReadyDelegate OnReady;
        void Ready()
        {
            OnReady?.Invoke();
        }

        #region Remote Sync
        public bool InvokeRPC<T>(ref T command)
            where T : IRpcCommand
        {
            if (Behaviours.TryGetValue(command.Behaviour, out var target) == false)
            {
                Debug.LogWarning($"No Behaviour with ID {command.Behaviour} found to Invoke RPC");
                return false;
            }

            return target.InvokeRPC(ref command);
        }

        public void InvokeSyncVar(SyncVarCommand command)
        {
            if (Behaviours.TryGetValue(command.Behaviour, out var target) == false)
            {
                Debug.LogWarning($"No Behaviour with ID {command.Behaviour} found to invoke RPC");
                return;
            }

            target.InvokeSyncVar(command);
        }
        #endregion

        public delegate void DespawnDelegate();
        public event DespawnDelegate OnDespawn;
        internal virtual void Despawn()
        {
            IsReady = false;

            OnDespawn?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            if (UnityScene.isLoaded && Application.isPlaying == false) NetworkScene.UnregisterLocal(this);

            NetworkAPI.Realtime.OnBufferEnd -= ReadyAfterBufferCallback;
        }

        //Static Utility
        public static bool IsAttachedTo(GameObject gameObject) => gameObject.TryGetComponent<NetworkEntity>(out _);

        public static NetworkEntity Find(NetworkEntityID id)
        {
            TryFind(id, out var entity);

            return entity;
        }

        public static bool TryFind(NetworkEntityID id, out NetworkEntity entity)
        {
            if (NetworkAPI.Client.IsConnected == false)
            {
                entity = null;
                return false;
            }

            if (NetworkAPI.Room.Entities.TryGet(id, out entity)) return true;

            return false;
        }

        public static bool CheckIfMasterObject(EntityType type) => type == EntityType.SceneObject || type == EntityType.Orphan;

        public static NetworkEntity ResolveComponent(GameObject gameObject)
        {
            var component = QueryComponent.InParents<NetworkEntity>(gameObject);

#if UNITY_EDITOR
            if (component == null)
            {
                component = gameObject.AddComponent<NetworkEntity>();
                ComponentUtility.MoveComponentUp(component);
            }
#endif
            return component;
        }
    }
}