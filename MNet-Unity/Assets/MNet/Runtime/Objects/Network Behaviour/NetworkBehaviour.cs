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

namespace MNet
{
    public abstract partial class NetworkBehaviour : MonoBehaviour
    {
        public NetworkBehaviourID ID { get; protected set; }

        public NetworkEntity Entity { get; protected set; }

        /// <summary>
        /// Reflects 'IsReady' to Component's 'enabled' Property,
        /// set by default to match the Behaviour's initial enabled state,
        /// ensures that (Start, Update, Fixed Update, ... etc) are only executed when the Entity is Spawned,
        /// ovverride to False if normal Unity callback behaviour is desired
        /// </summary>
        public virtual bool ReflectReadyToEnable => initialEnableState;

        bool initialEnableState;

        /// <summary>
        /// Token to use for cancelling Async Tasks on despawn
        /// </summary>
        protected CancellationTokenSource DespawnCancellation { get; private set; }

        protected virtual void Reset()
        {
#if UNITY_EDITOR
            ResolveEntity();
#endif
        }

        #region Setup
        internal void Setup(NetworkEntity entity, NetworkBehaviourID id)
        {
            this.ID = id;

            this.Entity = entity;

            initialEnableState = enabled;

            DespawnCancellation = new CancellationTokenSource();

            ParseRPCs();
            ParseSyncVars();

            UpdateReadyState();

            Entity.OnOwnerSet += OwnerSetCallback;
            Entity.OnSpawn += SpawnCallback;
            Entity.OnDespawn += DespawnCallback;

            OnSetup();
        }

        /// <summary>
        /// Simillar to Awake, invoked before OnLoad
        /// </summary>
        protected virtual void OnSetup() { }
        #endregion

        #region Load
        internal void Load()
        {
            UpdateReadyState();

            OnLoad();
        }

        /// <summary>
        /// Called when Behaviour is loaded in the network, invoked after OnSetup,
        /// by this point the behaviour should be ready to send and recieve network messages
        /// but if this behaviour is buffered then it's buffered RPCs and SyncVars wouldn't be set yet,
        /// Use OnSpawn for a callback where those will be set
        /// </summary>
        protected virtual void OnLoad() { }
        #endregion

        void UpdateReadyState()
        {
            if (ReflectReadyToEnable == false) return;

            enabled = Entity == null ? false : Entity.IsReady;
        }

        #region Set Owner
        void OwnerSetCallback(NetworkClient client)
        {
            OnOwnerSet(client);
        }

        /// <summary>
        /// Invoked when entity owner is set, such as when the entity is spawned, made orphan, has its owner changed, ... etc
        /// </summary>
        /// <param name="client"></param>
        protected virtual void OnOwnerSet(NetworkClient client)
        {

        }
        #endregion

        #region Spawn
        void SpawnCallback()
        {
            UpdateReadyState();

            OnSpawn();
        }

        /// <summary>
        /// Invoked when this behaviour is spawned and ready for use,
        /// entity will have all its buffered data applied and ready
        /// </summary>
        protected virtual void OnSpawn() { }
        #endregion

        #region Despawn
        void DespawnCallback()
        {
            UpdateReadyState();

            DespawnCancellation.Cancel();
            DespawnCancellation.Dispose();

            OnDespawn();
        }

        /// <summary>
        /// Invoked when the entity is despawned from the network
        /// </summary>
        protected virtual void OnDespawn() { }
        #endregion

        #region RPC
        protected DualDictionary<RpxMethodID, string, RpcBind> RPCs { get; private set; }

        void ParseRPCs()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var type = GetType();

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
        protected bool BroadcastRPC(MethodInfo method, RemoteBufferMode buffer, NetworkClient exception, params object[] arguments)
        {
            var name = RpcBind.GetName(method);

            return BroadcastRPC(name, buffer, exception, arguments);
        }
        protected bool BroadcastRPC(string method, RemoteBufferMode buffer, NetworkClient exception, params object[] arguments)
        {
            if (RPCs.TryGetValue(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return false;
            }

            var payload = RpcRequest.WriteBroadcast(Entity.ID, ID, bind.MethodID, buffer, arguments);

            if (exception != null) payload.Except(exception.ID);

            return SendRPC(bind, payload);
        }

        protected bool TargetRPC(MethodInfo method, NetworkClient target, params object[] arguments)
        {
            var name = RpcBind.GetName(method);

            return TargetRPC(name, target, arguments);
        }
        protected bool TargetRPC(string method, NetworkClient target, params object[] arguments)
        {
            if (RPCs.TryGetValue(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return false;
            }

            var payload = RpcRequest.WriteTarget(Entity.ID, ID, bind.MethodID, target.ID, arguments);

            return SendRPC(bind, payload);
        }

        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult>(MethodInfo method, NetworkClient target, params object[] arguments)
        {
            var name = RpcBind.GetName(method);

            return QueryRPC<TResult>(name, target, arguments);
        }
        protected async UniTask<RprAnswer<TResult>> QueryRPC<TResult>(string method, NetworkClient target, params object[] arguments)
        {
            if (RPCs.TryGetValue(method, out var bind) == false)
            {
                Debug.LogError($"No RPC With Name '{method}' Found on Entity '{Entity}'");
                return new RprAnswer<TResult>(RemoteResponseType.FatalFailure);
            }

            var promise = NetworkAPI.Client.RPR.Promise(target);

            var payload = RpcRequest.WriteQuery(Entity.ID, ID, bind.MethodID, target.ID, promise.Channel, arguments);

            if (SendRPC(bind, payload) == false)
            {
                Debug.LogError($"Couldn't Send Query RPC {method} to {target}");
                return new RprAnswer<TResult>(RemoteResponseType.FatalFailure);
            }

            await UniTask.WaitUntil(promise.IsComplete);

            var answer = new RprAnswer<TResult>(promise);

            return answer;
        }

        internal bool SendRPC(RpcBind bind, RpcRequest request)
        {
            if (NetworkAPI.Client.IsConnected == false)
            {
                Debug.LogWarning($"Attempting to Send RPC {request} when Client is not Connected, Ignoring");
                return false;
            }

            if (ValidateAuthority(NetworkAPI.Client.ID, bind.Authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'");
                return false;
            }

            return Send(ref request, bind.DeliveryMode);
        }
        #endregion

        internal void InvokeRPC(RpcCommand command)
        {
            if (RPCs.TryGetValue(command.Method, out var bind) == false)
            {
                Debug.LogError($"Can't Invoke Non-Existant RPC '{GetType().Name}->{command.Method}'");
                if (command.Type == RpcType.Query) NetworkAPI.Client.RPR.Respond(command, RemoteResponseType.FatalFailure);
                return;
            }

            if (ValidateAuthority(command.Sender, bind.Authority) == false)
            {
                Debug.LogWarning($"RPC Command for '{bind}' with Invalid Authority Recieved From Client '{command.Sender}'");
                if (command.Type == RpcType.Query) NetworkAPI.Client.RPR.Respond(command, RemoteResponseType.FatalFailure);
                return;
            }

            object[] arguments;
            try
            {
                arguments = bind.ParseArguments(command);
            }
            catch (Exception ex)
            {
                var text = $"Error trying to Parse RPC Arguments of {bind}', Invalid Data Sent Most Likely \n" +
                    $"Exception: \n" +
                    $"{ex}";

                Debug.LogError(text, this);
                if (command.Type == RpcType.Query) NetworkAPI.Client.RPR.Respond(command, RemoteResponseType.FatalFailure);
                return;
            }

            object result;
            try
            {
                result = bind.Invoke(arguments);
            }
            catch (Exception ex)
            {
                var text = $"Error Trying to Invoke RPC {bind}', " +
                    $"Please Ensure Method is Implemented And Invoked Correctly\n" +
                    $"Exception: \n" +
                    $"{ex}";

                Debug.LogError(text, this);
                if (command.Type == RpcType.Query) NetworkAPI.Client.RPR.Respond(command, RemoteResponseType.FatalFailure);
                return;
            }

            if (command.Type == RpcType.Query)
            {
                if (bind.IsAsync)
                    AwaitAsyncQueryRPC(result as IUniTask, command, bind).Forget();
                else
                    NetworkAPI.Client.RPR.Respond(command.Sender, command.ReturnChannel, result, bind.ReturnType);
            }
            else
            {
                if (bind.IsCoroutine)
                    ExceuteCoroutineRPC(result as IEnumerator);
            }
        }

        void ExceuteCoroutineRPC(IEnumerator method)
        {
            StartCoroutine(method);
        }

        async UniTask AwaitAsyncQueryRPC(IUniTask task, RpcCommand command, RpcBind bind)
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

            NetworkAPI.Client.RPR.Respond(command.Sender, command.ReturnChannel, task.Result, task.Type);
        }
        #endregion

        #region SyncVar
        protected DualDictionary<SyncVarFieldID, string, SyncVarBind> SyncVars { get; private set; }

        void ParseSyncVars()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var type = GetType();

            var properties = type.GetProperties(flags).Where(SyncVarAttribute.Defined).OrderBy(SyncVarBind.GetName).ToArray();

            if (properties.Length > byte.MaxValue)
                throw new Exception($"NetworkBehaviour {GetType().Name} Can't Have More than {byte.MaxValue} SyncVars Defined");

            for (byte i = 0; i < properties.Length; i++)
            {
                var attribute = SyncVarAttribute.Retrieve(properties[i]);

                var bind = new SyncVarBind(this, attribute, properties[i], i);

                if (SyncVars.Contains(bind.Name))
                    throw new Exception($"SyncVar Named {bind.Name} Already Registered On Behaviour {type}, Please Assign Every SyncVar a Unique Name");

                SyncVars.Add(bind.FieldID, bind.Name, bind);
            }
        }

        #region Methods
        /// <summary>
        /// Overload for ensuring type safety
        /// </summary>
        protected bool SyncVar<T>(string name, T field, T value) => SyncVar(name, value);

        protected bool SyncVar(string variable, object value)
        {
            if (SyncVars.TryGetValue(variable, out var bind) == false)
            {
                Debug.LogError($"No SyncVar Found With Name {variable}");
                return false;
            }

            var request = bind.CreateRequest(value);

            return SendSyncVar(bind, request);
        }
        #endregion

        internal bool SendSyncVar(SyncVarBind bind, SyncVarRequest request)
        {
            if (NetworkAPI.Client.IsConnected == false)
            {
                Debug.LogWarning($"Attempting to Send SyncVar {request} when Client is not Connected, Ignoring");
                return false;
            }

            if (ValidateAuthority(NetworkAPI.Client.ID, bind.Authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Set SyncVar '{bind}'");
                return false;
            }

            return Send(ref request, bind.DeliveryMode);
        }

        internal void InvokeSyncVar(SyncVarCommand command)
        {
            if (SyncVars.TryGetValue(command.Field, out var bind) == false)
            {
                Debug.LogWarning($"No SyncVar '{GetType().Name}->{command.Field}' Found on to Invoke");
                return;
            }

            if (ValidateAuthority(command.Sender, bind.Authority) == false)
            {
                Debug.LogWarning($"SyncVar '{bind}' with Invalid Authority Recieved From Client '{command.Sender}'");
                return;
            }

            object value;
            try
            {
                value = bind.ParseValue(command);
            }
            catch (Exception ex)
            {
                var text = $"Error trying to Parse Value for SyncVar '{bind}', Invalid Data Sent Most Likely \n" +
                    $"Exception: \n" +
                    $"{ex}";

                Debug.LogWarning(text, this);
                return;
            }

            try
            {
                bind.Set(value);
            }
            catch (Exception)
            {
                var text = $"Error Trying to Set SyncVar '{bind}' With Value '{value}', " +
                    $"Please Ensure SyncVar is Implemented Correctly";

                Debug.LogWarning(text, this);
                return;
            }
        }
        #endregion

        public bool ValidateAuthority(NetworkClientID sender, RemoteAuthority authority)
        {
            //instantly validate every buffered message
            if (NetworkAPI.Realtime.IsOnBuffer) return true;

            if (authority.HasFlag(RemoteAuthority.Any)) return true;

            if (authority.HasFlag(RemoteAuthority.Owner))
            {
                if (sender == Entity.Owner?.ID)
                    return true;
            }

            if (authority.HasFlag(RemoteAuthority.Master))
            {
                if (sender == NetworkAPI.Room.Master?.ID)
                    return true;
            }

            return false;
        }

        protected virtual bool Send<T>(ref T payload, DeliveryMode mode = DeliveryMode.Reliable)
        {
            if (Entity.IsReady == false)
            {
                Debug.LogError($"Trying to Send Payload '{payload}' Before Entity '{this}' is Marked Ready, Please Wait for Ready Or Override {nameof(OnSpawn)}");
                return false;
            }

            return NetworkAPI.Client.Send(ref payload, mode);
        }

        #region Editor
#if UNITY_EDITOR
        void ResolveEntity()
        {
            var entity = Dependancy.Get<NetworkEntity>(gameObject, Dependancy.Scope.CurrentToParents);

            if (entity == null)
            {
                entity = gameObject.AddComponent<NetworkEntity>();
                ComponentUtility.MoveComponentUp(entity);
            }
        }
#endif
        #endregion

        public NetworkBehaviour()
        {
            RPCs = new DualDictionary<RpxMethodID, string, RpcBind>();

            SyncVars = new DualDictionary<SyncVarFieldID, string, SyncVarBind>();
        }
    }
}