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

namespace MNet
{
    public abstract partial class NetworkBehaviour : MonoBehaviour
    {
        public NetworkBehaviourID ID { get; protected set; }

        public NetworkEntity Entity { get; protected set; }

        public bool IsMine => Entity == null ? false : Entity.IsMine;
        public bool IsReady => Entity == null ? false : Entity.IsReady;

        public NetworkClient Owner => Entity?.Owner;
        public AttributesCollection Attributes => Entity?.Attributes;

        /// <summary>
        /// Reflects 'IsReady' to Component's 'enabled' Property,
        /// Set to True by default to ensures that (Start, Update, Fixed Update, ... etc) are only executed when the Entity is Spawned,
        /// Override to False if normal Unity callback behaviour is desired
        /// </summary>
        public virtual bool ReflectReadyToEnable => true;

        protected virtual void Reset()
        {
            ResolveEntity();
        }

        void ResolveEntity()
        {
#if UNITY_EDITOR
            var entity = Dependancy.Get<NetworkEntity>(gameObject, Dependancy.Scope.CurrentToParents);

            if (entity == null)
            {
                entity = gameObject.AddComponent<NetworkEntity>();
                ComponentUtility.MoveComponentUp(entity);
            }
#endif
        }

        public void UpdateReadyState()
        {
            if (ReflectReadyToEnable == false) return;

            enabled = IsReady;
        }

        public virtual void Configure(NetworkEntity entity, NetworkBehaviourID id)
        {
            this.Entity = entity;
            this.ID = id;

            ParseRPCs();
            ParseSyncVars();

            UpdateReadyState();

            entity.OnSpawn += SpawnCallback;
            entity.OnDespawn += DespawnCallback;
        }

        #region Spawn
        void SpawnCallback()
        {
            UpdateReadyState();

            OnSpawn();
        }

        protected virtual void OnSpawn() { }
        #endregion

        #region Despawn
        void DespawnCallback()
        {
            UpdateReadyState();

            OnDespawn();
        }

        protected virtual void OnDespawn() { }
        #endregion

        #region RPC
        protected Dictionary<string, RpcBind> RPCs { get; private set; }

        void ParseRPCs()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var type = GetType();

            foreach (var method in type.GetMethods(flags))
            {
                var attribute = method.GetCustomAttribute<NetworkRPCAttribute>();

                if (attribute == null) continue;

                var bind = new RpcBind(this, attribute, method);

                if (RPCs.ContainsKey(bind.Name))
                    throw new Exception($"Rpc '{bind.Name}' Already Registered On '{GetType()}', Please Assign Every RPC a Unique Name And Don't Overload RPC Methods");

                RPCs.Add(bind.Name, bind);
            }
        }

        bool FindRPC(RpxMethodID method, out RpcBind bind) => FindRPC(method.Value, out bind);
        bool FindRPC(string name, out RpcBind bind) => RPCs.TryGetValue(name, out bind);

        #region Methods
        protected bool BroadcastRPC(string method, RpcBufferMode buffer = RpcBufferMode.None, NetworkClientID? exception = null, params object[] arguments)
        {
            if (FindRPC(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return false;
            }

            var payload = bind.CreateRequest(buffer, arguments);

            if (exception.HasValue) payload.Except(exception.Value);

            return SendRPC(bind, payload);
        }

        protected bool TargetRPC(string method, NetworkClient target, params object[] arguments)
        {
            if (FindRPC(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return false;
            }

            var payload = bind.CreateRequest(target.ID, arguments);

            return SendRPC(bind, payload);
        }

        protected bool ReturnRPC<TResult>(string method, NetworkClient target, RprCallback<TResult> result, params object[] arguments)
        {
            if (FindRPC(method, out var bind) == false)
            {
                Debug.LogError($"No RPC With Name '{method}' Found on Entity '{Entity}'");
                return false;
            }

            if (bind.ReturnType != typeof(TResult))
            {
                Debug.LogError($"RPC '{bind}' has Mismatched RPR Return Types '{bind.ReturnType.Name}' vs '{typeof(TResult).Name}'");
                return false;
            }

            var rpr = Entity.RegisterRPR(result.Method, result.Target);

            var payload = bind.CreateRequest(target.ID, rpr.ID, arguments);

            return SendRPC(bind, payload);
        }

        bool SendRPC(RpcBind bind, RpcRequest request)
        {
            if (ValidateAuthority(NetworkAPI.Client.ID, bind.Authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'");
                return false;
            }

            return Send(request, bind.DeliveryMode);
        }
        #endregion

        internal void InvokeRPC(RpcCommand command)
        {
            if (FindRPC(command.Method, out var bind) == false)
            {
                Debug.LogWarning($"Can't Invoke Non-Existant RPC '{command.Method}' On '{GetType()}'");

                ResolveRPC(command, RprResult.MethodNotFound);

                return;
            }

            if (ValidateAuthority(command.Sender, bind.Authority) == false)
            {
                Debug.LogWarning($"RPC '{bind}' with Invalid Authority Recieved From Client '{command.Sender}'");

                ResolveRPC(command, RprResult.InvalidAuthority);

                return;
            }

            object[] arguments;
            try
            {
                arguments = bind.ParseArguments(command);
            }
            catch (Exception e)
            {
                var text = $"Error trying to Parse RPC Arguments of Method '{command.Method}' on '{this}', Invalid Data Sent Most Likely \n" +
                    $"Exception: \n" +
                    $"{e}";

                Debug.LogWarning(text, this);

                ResolveRPC(command, RprResult.InvalidArguments);

                return;
            }

            object result;
            try
            {
                result = bind.Invoke(arguments);
            }
            catch (TargetInvocationException)
            {
                ResolveRPC(command, RprResult.RuntimeException);
                throw;
            }
            catch (Exception)
            {
                var text = $"Error Trying to Invoke RPC Method '{command.Method}' on '{this}', " +
                    $"Please Ensure Method is Implemented And Invoked Correctly";

                Debug.LogWarning(text, this);

                ResolveRPC(command, RprResult.RuntimeException);

                return;
            }

            if (command.Type == RpcType.Return) SendRPR(command, result);
        }

        void ResolveRPC(RpcCommand command, RprResult result) => NetworkAPI.Room.ResolveRPC(command, result);
        #endregion

        #region RPR
        void SendRPR(RpcCommand rpc, object value)
        {
            var payload = RprRequest.Write(rpc.Entity, rpc.Sender, rpc.Callback, value);

            NetworkAPI.Client.Send(payload);
        }
        #endregion

        #region SyncVar
        protected Dictionary<string, SyncVarBind> SyncVars { get; private set; }

        void ParseSyncVars()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var type = GetType();

            foreach (var field in type.GetFields(flags))
            {
                var attribute = field.GetCustomAttribute<SyncVarAttribute>();

                if (attribute == null) continue;

                var bind = new SyncVarBind(this, attribute, field);

                if (SyncVars.ContainsKey(bind.Name))
                    throw new Exception($"SyncVar Named {bind.Name} Already Registered On Behaviour {type}, Please Assign Every SyncVar a Unique Name");

                SyncVars.Add(bind.Name, bind);
            }

            foreach (var property in type.GetProperties(flags))
            {
                var attribute = property.GetCustomAttribute<SyncVarAttribute>();

                if (attribute == null) continue;

                var bind = new SyncVarBind(this, attribute, property);

                if (SyncVars.ContainsKey(bind.Name))
                    throw new Exception($"SyncVar Named {bind.Name} Already Registered On Behaviour {type}, Please Assign Every SyncVar a Unique Name");

                SyncVars.Add(bind.Name, bind);
            }
        }

        bool FindSyncVar(string variable, out SyncVarBind bind) => SyncVars.TryGetValue(variable, out bind);

        #region Methods
        /// <summary>
        /// Overload for ensuring type safety
        /// </summary>
        protected bool SyncVar<T>(string name, T field, T value) => SyncVar(name, value);

        protected bool SyncVar(string variable, object value)
        {
            if (FindSyncVar(variable, out var bind) == false)
            {
                Debug.LogError($"No SyncVar Found With Name {variable}");
                return false;
            }

            var request = bind.CreateRequest(value);

            return SyncVar(bind, request);
        }

        bool SyncVar(SyncVarBind bind, SyncVarRequest request)
        {
            if (ValidateAuthority(NetworkAPI.Client.ID, bind.Authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Set SyncVar '{bind}'");
                return false;
            }

            return Send(request, bind.DeliveryMode);
        }
        #endregion

        internal void InvokeSyncVar(SyncVarCommand command)
        {
            if (SyncVars.TryGetValue(command.Variable, out var bind) == false)
            {
                Debug.LogWarning($"No SyncVar {command.Variable} Found to Invoke");
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
                var text = $"Error trying to Parse Value for SyncVar '{command.Variable}' on {this}, Invalid Data Sent Most Likely \n" +
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
                var text = $"Error Trying to Set SyncVar '{command.Variable}' on '{this} With Value '{value}', " +
                    $"Please Ensure SyncVar is Implemented Correctly";

                Debug.LogWarning(text, this);
                return;
            }
        }
        #endregion

        public bool ValidateAuthority(NetworkClientID sender, RemoteAuthority authority)
        {
            //instantly validate every buffered message
            if (NetworkAPI.Room.IsApplyingMessageBuffer) return true;

            if (authority.HasFlag(RemoteAuthority.Any)) return true;

            if (authority.HasFlag(RemoteAuthority.Owner))
            {
                if (sender == Owner?.ID)
                    return true;
            }

            if (authority.HasFlag(RemoteAuthority.Master))
            {
                if (sender == NetworkAPI.Room.Master?.ID)
                    return true;
            }

            return false;
        }

        protected virtual bool Send<T>(T payload, DeliveryMode mode = DeliveryMode.Reliable)
        {
            if (IsReady == false)
            {
                Debug.LogError($"Trying to Send Payload '{payload}' Before Entity '{this}' is Marked Ready, Please Wait for Ready Or Override {nameof(OnSpawn)}");
                return false;
            }

            return NetworkAPI.Client.Send(payload, mode);
        }

        public NetworkBehaviour()
        {
            RPCs = new Dictionary<string, RpcBind>();

            SyncVars = new Dictionary<string, SyncVarBind>();
        }
    }
}