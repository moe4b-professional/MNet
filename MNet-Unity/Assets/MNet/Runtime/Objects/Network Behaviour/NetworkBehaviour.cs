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
        /// Token to use for cancelling Async Tasks on despawn
        /// </summary>
        protected CancellationTokenSource ASyncDespawnCancellation { get; private set; }

        protected virtual void Reset()
        {
#if UNITY_EDITOR
            Entity = NetworkEntity.ResolveComponent(gameObject);
#endif
        }

        internal void Set(NetworkEntity entity, NetworkBehaviourID id)
        {
            this.ID = id;
            this.Entity = entity;

            ASyncDespawnCancellation = new CancellationTokenSource();

            ParseRPCs();
            ParseSyncVars();
        }

        #region Setup
        internal void Setup()
        {
            OnSetup();
        }

        /// <summary>
        /// Stage 1 on entity startup procedure,
        /// called when entity has its properties set (owner, type, persistance, attributes ... etc),
        /// entity still not ready to send and recieve messages and doesn't have a valid ID,
        /// useful for applying attributes and the like
        /// </summary>
        protected virtual void OnSetup() { }
        #endregion

        #region Set Owner
        internal void SetOwner(NetworkClient client)
        {
            OnOwnerSet(client);
        }

        /// <summary>
        /// Invoked when entity owner is set, such as when the entity is spawned, made orphan, has its owner changed, ... etc
        /// </summary>
        /// <param name="client"></param>
        protected virtual void OnOwnerSet(NetworkClient client) { }
        #endregion

        #region Spawn
        internal void Spawn()
        {
            OnSpawn();
        }

        /// <summary>
        /// Stage 2 on the entity startup procedure,
        /// called when Behaviour is spawned on the network,
        /// by this point the behaviour has a valid ID and is ready to send and recieve messages
        /// but if this behaviour is buffered then its buffered RPCs and SyncVars wouldn't be set yet,
        /// use OnReady for a callback where SyncVars and RPCs will be set
        /// </summary>
        protected virtual void OnSpawn() { }
        #endregion

        #region Ready
        internal void Ready()
        {
            OnReady();
        }

        /// <summary>
        /// Stage 3 on the entity startup procedure,
        /// Invoked when this behaviour is spawned and ready for use,
        /// entity will have all its buffered data applied and ready
        /// </summary>
        protected virtual void OnReady() { }
        #endregion

        #region RPC
        protected DualDictionary<RpcMethodID, string, RpcBind> RPCs { get; private set; }

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
        protected bool BroadcastRPC(string method, RemoteBufferMode buffer, DeliveryMode delivery, NetworkGroupID group, NetworkClient exception, params object[] arguments)
        {
            if (RPCs.TryGetValue(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return false;
            }

            if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'");
                return false;
            }

            var raw = bind.WriteArguments(arguments);

            var request = BroadcastRpcRequest.Write(Entity.ID, ID, bind.MethodID, buffer, group, exception?.ID, raw);

            return Send(ref request, delivery);
        }

        protected bool TargetRPC(string method, NetworkClient target, DeliveryMode delivery, params object[] arguments)
        {
            if (RPCs.TryGetValue(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return false;
            }

            if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'");
                return false;
            }

            var raw = bind.WriteArguments(arguments);

            var request = TargetRpcRequest.Write(Entity.ID, ID, bind.MethodID, target.ID, raw);

            return Send(ref request, delivery);
        }

        protected async UniTask<RprAnswer<TResult>> QueryRPC<TResult>(string method, NetworkClient target, DeliveryMode delivery, params object[] arguments)
        {
            if (RPCs.TryGetValue(method, out var bind) == false)
            {
                Debug.LogError($"No RPC With Name '{method}' Found on Entity '{Entity}'");
                return new RprAnswer<TResult>(RemoteResponseType.FatalFailure);
            }

            if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'");
                return new RprAnswer<TResult>(RemoteResponseType.FatalFailure);
            }

            var promise = NetworkAPI.Client.RPR.Promise(target);

            var raw = bind.WriteArguments(arguments);

            var request = QueryRpcRequest.Write(Entity.ID, ID, bind.MethodID, target.ID, promise.Channel, raw);

            if (Send(ref request, delivery) == false)
            {
                Debug.LogError($"Couldn't Send Query RPC {method} to {target}");
                return new RprAnswer<TResult>(RemoteResponseType.FatalFailure);
            }

            await UniTask.WaitUntil(promise.IsComplete);

            var answer = new RprAnswer<TResult>(promise);

            return answer;
        }

        protected bool BufferRPC(string method, RemoteBufferMode buffer, DeliveryMode delivery, params object[] arguments)
        {
            if (RPCs.TryGetValue(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return false;
            }

            if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'");
                return false;
            }

            var raw = bind.WriteArguments(arguments);

            var request = BufferRpcRequest.Write(Entity.ID, ID, bind.MethodID, buffer, raw);

            return Send(ref request, delivery);
        }
        #endregion

        internal bool InvokeRPC<T>(ref T command)
            where T : IRpcCommand
        {
            if (RPCs.TryGetValue(command.Method, out var bind) == false)
            {
                Debug.LogError($"Can't Invoke Non-Existant RPC '{GetType().Name}->{command.Method}'");
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

                Debug.LogError(text, this);
                return false;
            }

            if (Entity.CheckAuthority(info.Sender, bind.Authority) == false)
            {
                Debug.LogWarning($"RPC Command for '{bind}' with Invalid Authority Recieved From Client '{command.Sender}'");
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

                Debug.LogError(text, this);

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

        void ExceuteCoroutineRPC(IEnumerator method) => StartCoroutine(method);

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
        protected DualDictionary<SyncVarFieldID, string, SyncVarBind> SyncVars { get; private set; }

        void ParseSyncVars()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var type = GetType();

            var fields = type.GetFields(flags).Cast<MemberInfo>();
            var properties = type.GetProperties(flags).Cast<MemberInfo>();

            var members = fields.Union(properties).Where(SyncVarAttribute.Defined).OrderBy(SyncVarBind.GetName).ToArray();

            if (members.Length > byte.MaxValue)
                throw new Exception($"NetworkBehaviour {GetType().Name} Can't Have More than {byte.MaxValue} SyncVars Defined");

            for (byte i = 0; i < members.Length; i++)
            {
                var attribute = SyncVarAttribute.Retrieve(members[i]);

                var bind = new SyncVarBind(this, attribute, members[i], i);

                if (SyncVars.Contains(bind.Name))
                    throw new Exception($"SyncVar Named {bind.Name} Already Registered On Behaviour {type}, Please Assign Every SyncVar a Unique Name");

                SyncVars.Add(bind.FieldID, bind.Name, bind);
            }
        }

        #region Methods
        /// <summary>
        /// Overload for ensuring type safety
        /// </summary>
        protected bool BroadcastSyncVar<T>(string name, T field, T value, DeliveryMode delivery = DeliveryMode.Reliable, NetworkGroupID group = default)
        {
            return BroadcastSyncVar(name, value, delivery, group);
        }
        protected bool BroadcastSyncVar(string name, object value, DeliveryMode delivery = DeliveryMode.Reliable, NetworkGroupID group = default)
        {
            if (SyncVars.TryGetValue(name, out var bind) == false)
            {
                Debug.LogError($"No SyncVar Found With Name {name}");
                return false;
            }

            if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Set SyncVar '{bind}'");
                return false;
            }

            var request = BroadcastSyncVarRequest.Write(Entity.ID, ID, bind.FieldID, group, value);

            return Send(ref request, delivery);
        }

        protected bool BufferSyncVar<T>(string name, T field, T value, DeliveryMode delivery = DeliveryMode.Reliable, NetworkGroupID group = default)
        {
            return BufferSyncVar(name, value, delivery, group);
        }
        protected bool BufferSyncVar(string name, object value, DeliveryMode delivery = DeliveryMode.Reliable, NetworkGroupID group = default)
        {
            if (SyncVars.TryGetValue(name, out var bind) == false)
            {
                Debug.LogError($"No SyncVar Found With Name {name}");
                return false;
            }

            if (Entity.CheckAuthority(NetworkAPI.Client.Self, bind.Authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Set SyncVar '{bind}'");
                return false;
            }

            var request = BufferSyncVarRequest.Write(Entity.ID, ID, bind.FieldID, group, value);

            return Send(ref request, delivery);
        }
        #endregion

        #region Hooks
        protected void RegisterSyncVarHook<T>(string name, T field, SyncVarHook<T> hook)
        {
            RegisterSyncVarHook<T>(name, hook);
        }
        protected void RegisterSyncVarHook<T>(string name, SyncVarHook<T> hook)
        {
            if (SyncVars.TryGetValue(name, out var bind) == false)
            {
                Debug.LogWarning($"No SyncVar '{GetType().Name}->{name}' Found on Register Hook on");
                return;
            }

            if (bind.Hooks.Contains(hook))
            {
                Debug.LogWarning($"SyncVar Hook {hook.Method.Name} Already Registered for SyncVar '{GetType().Name}->{name}' Cannot Register Hook More than Once");
                return;
            }

            bind.Hooks.Add(hook);
        }

        protected void UnregisterSyncVarHook<T>(string name, T field, SyncVarHook<T> hook)
        {
            UnregisterSyncVarHook(name, hook);
        }
        protected void UnregisterSyncVarHook<T>(string name, SyncVarHook<T> hook)
        {
            if (SyncVars.TryGetValue(name, out var bind) == false)
            {
                Debug.LogWarning($"No SyncVar '{GetType().Name}->{name}' Found on Register Hook on");
                return;
            }

            if (bind.Hooks.Remove(hook) == false)
                Debug.LogWarning($"No SyncVar Hook {hook.Method.Name} for SyncVar '{GetType().Name}->{name}' was Removed because it wasn't Registered to begin With");
        }
        #endregion

        internal void InvokeSyncVar(SyncVarCommand command)
        {
            if (SyncVars.TryGetValue(command.Field, out var bind) == false)
            {
                Debug.LogWarning($"No SyncVar '{GetType().Name}->{command.Field}' Found on to Invoke");
                return;
            }

            var oldValue = bind.GetValue();

            object newValue;
            SyncVarInfo info;
            try
            {
                bind.ParseCommand(command, out newValue, out info);
            }
            catch (Exception ex)
            {
                var text = $"Error trying to Parse Value for SyncVar '{bind}', Invalid Data Sent Most Likely \n" +
                    $"Exception: \n" +
                    $"{ex}";

                Debug.LogWarning(text, this);
                return;
            }

            if (Entity.CheckAuthority(info.Sender, bind.Authority) == false)
            {
                Debug.LogWarning($"SyncVar '{bind}' with Invalid Authority Recieved From Client '{command.Sender}'");
                return;
            }

            try
            {
                bind.SetValue(newValue);
            }
            catch (Exception)
            {
                var text = $"Error Trying to Set SyncVar '{bind}' With Value '{newValue}', " +
                    $"Please Ensure SyncVar is Implemented Correctly";

                Debug.LogWarning(text, this);
                return;
            }

            try
            {
                bind.InvokeHooks(oldValue, newValue, info);
            }
            catch (Exception)
            {
                var text = $"Error Trying to Invoke SyncVar Hooks for '{bind}' With Values '{oldValue}'/'{newValue}', " +
                    $"Please Ensure SyncVar is Implemented Correctly";

                Debug.LogWarning(text, this);
                return;
            }
        }
        #endregion

        protected virtual bool Send<T>(ref T payload, DeliveryMode mode = DeliveryMode.Reliable)
        {
            if (Entity.IsReady == false)
            {
                Debug.LogError($"Trying to Send Payload '{payload}' Before Entity '{this}' is Marked Ready, Please Wait for Ready Or Override {nameof(OnSpawn)}");
                return false;
            }

            return NetworkAPI.Client.Send(ref payload, mode);
        }

        #region Despawn
        internal void Despawn()
        {
            ASyncDespawnCancellation.Cancel();
            ASyncDespawnCancellation.Dispose();

            OnDespawn();
        }

        /// <summary>
        /// Invoked when the entity is despawned from the network
        /// </summary>
        protected virtual void OnDespawn() { }
        #endregion

        public NetworkBehaviour()
        {
            RPCs = new DualDictionary<RpcMethodID, string, RpcBind>();

            SyncVars = new DualDictionary<SyncVarFieldID, string, SyncVarBind>();
        }
    }
}