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
        public bool IsConnected => Entity == null ? false : Entity.IsConnected;

        public NetworkClient Owner => Entity?.Owner;

        public NetworkClient Supervisor => Entity?.Supervisor;

        public AttributesCollection Attributes => Entity?.Attributes;

        /// <summary>
        /// Reflects 'IsReady' to Component's 'enabled' Property,
        /// set by default to match the Behaviour's initial enabled state,
        /// ensures that (Start, Update, Fixed Update, ... etc) are only executed when the Entity is Spawned,
        /// ovverride to False if normal Unity callback behaviour is desired
        /// </summary>
        public virtual bool ReflectReadyToEnable
        {
            get
            {
                if (initialEnableState.HasValue == false) initialEnableState = enabled;

                return initialEnableState.Value;
            }
        }

        bool? initialEnableState;

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

        internal void UpdateReadyState()
        {
            if (ReflectReadyToEnable == false) return;

            enabled = IsReady;
        }

        internal void Setup(NetworkEntity entity, NetworkBehaviourID id)
        {
            this.Entity = entity;
            this.ID = id;

            ParseRPCs();
            ParseSyncVars();

            UpdateReadyState();

            entity.OnOwnerSet += OwnerSetCallback;
            entity.OnSpawn += SpawnCallback;
            entity.OnDespawn += DespawnCallback;
        }

        #region Set Owner
        void OwnerSetCallback(NetworkClient client)
        {
            OnOwnerSet(client);
        }

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
        protected bool BroadcastRPC(string method, RpcBufferMode buffer = RpcBufferMode.None, NetworkClient exception = null, params object[] arguments)
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

        protected bool TargetRPC(string method, NetworkClientID target, params object[] arguments)
        {
            if (RPCs.TryGetValue(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return false;
            }

            var payload = RpcRequest.WriteTarget(Entity.ID, ID, bind.MethodID, target, arguments);

            return SendRPC(bind, payload);
        }

        protected bool QueryRPC(string method, NetworkClient target, string result, params object[] arguments)
        {
            if (RPCs.TryGetValue(method, out var bind) == false)
            {
                Debug.LogError($"No RPC With Name '{method}' Found on Entity '{Entity}'");
                return false;
            }

            if(RPCs.TryGetValue(result, out var callback) == false)
            {
                Debug.LogError($"No RPC With Name '{result}' Found on Entity '{Entity}' to use for Return, Please Define all Return Methods as RPCs");
                return false;
            }

            var payload = RpcRequest.WriteQuery(Entity.ID, ID, bind.MethodID, target.ID, callback.MethodID, arguments);

            return SendRPC(bind, payload);
        }

        protected bool ResponseRPC(RpcMethodID method, NetworkClientID target, params object[] arguments)
        {
            if (RPCs.TryGetValue(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return false;
            }

            var payload = RpcRequest.WriteResponse(Entity.ID, ID, bind.MethodID, target, arguments);

            return SendRPC(bind, payload);
        }
        #endregion

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

            return Send(request, bind.DeliveryMode);
        }

        internal void InvokeRPC(RpcCommand command)
        {
            if (RPCs.TryGetValue(command.Method, out var bind) == false)
            {
                Debug.LogWarning($"Can't Invoke Non-Existant RPC '{GetType().Name}->{command.Method}'");
                return;
            }

            if (ValidateAuthority(command.Sender, bind.Authority) == false)
            {
                Debug.LogWarning($"RPC '{bind}' with Invalid Authority Recieved From Client '{command.Sender}'");
                return;
            }

            object[] arguments;
            try
            {
                arguments = bind.ParseArguments(command);
            }
            catch (Exception e)
            {
                var text = $"Error trying to Parse RPC Arguments of {bind}', Invalid Data Sent Most Likely \n" +
                    $"Exception: \n" +
                    $"{e}";

                Debug.LogWarning(text, this);
                return;
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
            catch (Exception)
            {
                var text = $"Error Trying to Invoke RPC {bind}', " +
                    $"Please Ensure Method is Implemented And Invoked Correctly";

                Debug.LogWarning(text, this);
                return;
            }

            if (command.Type == RpcType.Query) ResponseRPC(command.Callback, command.Sender, RprResult.Success, result);
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

            return Send(request, bind.DeliveryMode);
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
            RPCs = new DualDictionary<RpcMethodID, string, RpcBind>();

            SyncVars = new DualDictionary<SyncVarFieldID, string, SyncVarBind>();
        }
    }
}