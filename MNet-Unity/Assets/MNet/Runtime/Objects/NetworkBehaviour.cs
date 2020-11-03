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
using System.Threading;

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
        /// Mirrors 'IsReady' to Component's 'enabled' Property,
        /// Set to True by default to ensures that (Start, Update, Fixed Update, ... etc) are only executed when the Entity is Spawned,
        /// Override to False if normal Unity callback behaviour is desired
        /// </summary>
        public virtual bool MirrorReadyToEnable => true;

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
            if (MirrorReadyToEnable == false) return;

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

        bool FindRPC(string name, out RpcBind bind) => RPCs.TryGetValue(name, out bind);

        #region Literal Methods
        protected bool RPC(string method, params object[] arguments) => RPC(method, RpcBufferMode.None, arguments);
        protected bool RPC(string method, RpcBufferMode bufferMode, params object[] arguments)
        {
            if (FindRPC(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return false;
            }

            var payload = bind.CreateRequest(bufferMode, arguments);

            return RPC(bind, payload);
        }

        protected bool RPC(string method, NetworkClient target, params object[] arguments)
        {
            if (FindRPC(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return false;
            }

            var payload = bind.CreateRequest(target.ID, arguments);

            return RPC(bind, payload);
        }

        protected bool RPC<TResult>(string method, NetworkClient target, RprCallback<TResult> result, params object[] arguments)
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

            return RPC(bind, payload);
        }

        bool RPC(RpcBind bind, RpcRequest request)
        {
            if(ValidateAuthority(NetworkAPI.Client.ID, bind.Authority) == false)
            {
                Debug.LogError($"Local Client has Insufficent Authority to Call RPC '{bind}'");
                return false;
            }

            return Send(request);
        }
        #endregion

        #region Generic Methods
        public void RPC(RpcMethod method)
            => RPC(method.Method.Name);
        public void RPC<T1>(RpcMethod<T1> method, T1 arg1)
            => RPC(method.Method.Name, arg1);
        public void RPC<T1, T2>(RpcMethod<T1, T2> method, T1 arg1, T2 arg2)
            => RPC(method.Method.Name, arg1, arg2);
        public void RPC<T1, T2, T3>(RpcMethod<T1, T2, T3> method, T1 arg1, T2 arg2, T3 arg3)
            => RPC(method.Method.Name, arg1, arg2, arg3);
        public void RPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => RPC(method.Method.Name, arg1, arg2, arg3, arg4);
        public void RPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => RPC(method.Method.Name, arg1, arg2, arg3, arg4, arg5);
        public void RPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => RPC(method.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6);

        public void RPC(RpcMethod method, RpcBufferMode bufferMode)
            => RPC(method.Method.Name, bufferMode);
        public void RPC<T1>(RpcMethod<T1> method, RpcBufferMode bufferMode, T1 arg1)
            => RPC(method.Method.Name, bufferMode, arg1);
        public void RPC<T1, T2>(RpcMethod<T1, T2> method, RpcBufferMode bufferMode, T1 arg1, T2 arg2)
            => RPC(method.Method.Name, bufferMode, arg1, arg2);
        public void RPC<T1, T2, T3>(RpcMethod<T1, T2, T3> method, RpcBufferMode bufferMode, T1 arg1, T2 arg2, T3 arg3)
            => RPC(method.Method.Name, bufferMode, arg1, arg2, arg3);
        public void RPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, RpcBufferMode bufferMode, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => RPC(method.Method.Name, bufferMode, arg1, arg2, arg3, arg4);
        public void RPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, RpcBufferMode bufferMode, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => RPC(method.Method.Name, bufferMode, arg1, arg2, arg3, arg4, arg5);
        public void RPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, RpcBufferMode bufferMode, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => RPC(method.Method.Name, bufferMode, arg1, arg2, arg3, arg4, arg5, arg6);

        public void RPC(RpcMethod method, NetworkClient target)
            => RPC(method.Method.Name, target);
        public void RPC<T1>(RpcMethod<T1> method, NetworkClient target, T1 arg1)
            => RPC(method.Method.Name, target, arg1);
        public void RPC<T1, T2>(RpcMethod<T1, T2> method, NetworkClient target, T1 arg1, T2 arg2)
            => RPC(method.Method.Name, target, arg1, arg2);
        public void RPC<T1, T2, T3>(RpcMethod<T1, T2, T3> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3)
            => RPC(method.Method.Name, target, arg1, arg2, arg3);
        public void RPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => RPC(method.Method.Name, target, arg1, arg2, arg3, arg4);
        public void RPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => RPC(method.Method.Name, target, arg1, arg2, arg3, arg4, arg5);
        public void RPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => RPC(method.Method.Name, target, arg1, arg2, arg3, arg4, arg5, arg6);

        public void RPC<TResult>(RprMethod<TResult> method, NetworkClient target, RprCallback<TResult> callback)
            => RPC(method.Method.Name, target, callback);
        public void RPC<TResult, T1>(RprMethod<TResult, T1> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1)
            => RPC(method.Method.Name, target, callback, arg1);
        public void RPC<TResult, T1, T2>(RprMethod<TResult, T1, T2> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2)
            => RPC(method.Method.Name, target, callback, arg1, arg2);
        public void RPC<TResult, T1, T2, T3>(RprMethod<TResult, T1, T2, T3> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2, T3 arg3)
            => RPC(method.Method.Name, target, callback, arg1, arg2, arg3);
        public void RPC<TResult, T1, T2, T3, T4>(RprMethod<TResult, T1, T2, T3, T4> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => RPC(method.Method.Name, target, callback, arg1, arg2, arg3, arg4);
        public void RPC<TResult, T1, T2, T3, T4, T5>(RprMethod<TResult, T1, T2, T3, T4, T5> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => RPC(method.Method.Name, target, callback, arg1, arg2, arg3, arg4, arg5);
        public void RPC<TResult, T1, T2, T3, T4, T5, T6>(RprMethod<TResult, T1, T2, T3, T4, T5, T6> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => RPC(method.Method.Name, target, callback, arg1, arg2, arg3, arg4, arg5, arg6);
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

            return Send(request);
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

        public bool ValidateAuthority(NetworkClientID sender, RemoteAutority authority)
        {
            //instantly validate every buffered message
            if (NetworkAPI.Room.IsApplyingMessageBuffer) return true;

            if (authority.HasFlag(RemoteAutority.Any)) return true;

            if (authority.HasFlag(RemoteAutority.Owner))
            {
                if (sender == Owner?.ID)
                    return true;
            }

            if (authority.HasFlag(RemoteAutority.Master))
            {
                if (sender == NetworkAPI.Room.Master?.ID)
                    return true;
            }

            return false;
        }

        protected virtual bool Send<T>(T payload)
        {
            if (IsReady == false)
            {
                Debug.LogError($"Trying to Send Payload '{payload}' Before Entity '{this}' is Marked Ready, Please Wait for Ready Or Override {nameof(OnSpawn)}");
                return false;
            }

            if (NetworkAPI.Client.IsConnected == false)
            {
                Debug.LogWarning($"Cannot Send Payload '{payload}' When Network Client Isn't Connected");
                return false;
            }

            return NetworkAPI.Client.Send(payload);
        }

        public NetworkBehaviour()
        {
            RPCs = new Dictionary<string, RpcBind>();

            SyncVars = new Dictionary<string, SyncVarBind>();
        }
    }
}