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
using System.Threading;

namespace MNet
{
    public partial class NetworkBehaviour : MonoBehaviour
	{
        public NetworkBehaviourID ID { get; protected set; }

        public NetworkEntity Entity { get; protected set; }

        public bool IsMine => Entity == null ? false : Entity.IsMine;
        public bool IsReady => Entity == null ? false : Entity.IsReady;

        public NetworkClient Owner => Entity == null ? null : Entity.Owner;

        public AttributesCollection Attributes => Entity == null ? null : Entity.Attributes;

        public virtual bool DisableOnUnready => true;

        public void Configure(NetworkEntity entity, NetworkBehaviourID id)
        {
            this.Entity = entity;
            this.ID = id;

            ParseRPCs();
            ParseSyncVars();

            if (DisableOnUnready) enabled = false;

            entity.OnSpawn += SpawnCallback;
            entity.OnDespawn += DespawnCallback;

            MNetAPI.Room.OnAppliedMessageBuffer += AppliedMessageBufferCallback;
        }

        void SpawnCallback()
        {
            enabled = true;

            OnSpawn();
        }
        protected virtual void OnSpawn() { }

        void AppliedMessageBufferCallback()
        {
            MNetAPI.Room.OnAppliedMessageBuffer -= AppliedMessageBufferCallback;

            OnAppliedMessageBuffer();
        }
        protected virtual void OnAppliedMessageBuffer() { }

        void DespawnCallback()
        {
            if (DisableOnUnready) enabled = false;

            OnDespawn();
        }
        protected virtual void OnDespawn() { }

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
                    throw new Exception($"Rpc Named {bind.Name} Already Registered On Behaviour {GetType()}, Please Assign Every RPC a Unique Name And Don't Overload the RPC Methods");

                RPCs.Add(bind.Name, bind);
            }
        }

        bool FindRPC(string name, out RpcBind bind) => RPCs.TryGetValue(name, out bind);

        protected void RequestRPC(string method, params object[] arguments) => RequestRPC(method, RpcBufferMode.None, arguments);
        protected void RequestRPC(string method, RpcBufferMode bufferMode, params object[] arguments)
        {
            if (FindRPC(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return;
            }

            var payload = bind.CreateRequest(bufferMode, arguments);

            SendRPC(payload);
        }

        protected void RequestRPC(string method, NetworkClient target, params object[] arguments)
        {
            if (FindRPC(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return;
            }

            var payload = bind.CreateRequest(target.ID, arguments);

            SendRPC(payload);
        }

        protected void RequestRPC<TResult>(string method, NetworkClient target, RprCallback<TResult> result, params object[] arguments)
        {
            if (FindRPC(method, out var bind) == false)
            {
                Debug.LogError($"No RPC Found With Name {method}");
                return;
            }

            var rpr = Entity.RegisterRPR(result.Method, result.Target);

            var payload = bind.CreateRequest(target.ID, rpr.ID, arguments);

            SendRPC(payload);
        }

        protected virtual void SendRPC(RpcRequest request)
        {
            if (IsReady == false)
            {
                Debug.LogError($"Trying to Invoke RPC {request.Method} on {name} Before It's Ready, Please Wait Untill IsReady or After OnSpawn Method");
                return;
            }

            if (MNetAPI.Client.IsConnected == false)
            {
                Debug.LogWarning($"Cannot Send RPC {request.Method} When Client Isn't Connected");
                return;
            }

            MNetAPI.Client.Send(request);
        }

        #region Generic Methods
        public void RequestRPC(RpcMethod method)
            => RequestRPC(method.Method.Name);
        public void RequestRPC<T1>(RpcMethod<T1> method, T1 arg1)
            => RequestRPC(method.Method.Name, arg1);
        public void RequestRPC<T1, T2>(RpcMethod<T1, T2> method, T1 arg1, T2 arg2)
            => RequestRPC(method.Method.Name, arg1, arg2);
        public void RequestRPC<T1, T2, T3>(RpcMethod<T1, T2, T3> method, T1 arg1, T2 arg2, T3 arg3)
            => RequestRPC(method.Method.Name, arg1, arg2, arg3);
        public void RequestRPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => RequestRPC(method.Method.Name, arg1, arg2, arg3, arg4);
        public void RequestRPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => RequestRPC(method.Method.Name, arg1, arg2, arg3, arg4, arg5);
        public void RequestRPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => RequestRPC(method.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6);

        public void RequestRPC(RpcMethod method, RpcBufferMode bufferMode)
            => RequestRPC(method.Method.Name, bufferMode);
        public void RequestRPC<T1>(RpcMethod<T1> method, RpcBufferMode bufferMode, T1 arg1)
            => RequestRPC(method.Method.Name, bufferMode, arg1);
        public void RequestRPC<T1, T2>(RpcMethod<T1, T2> method, RpcBufferMode bufferMode, T1 arg1, T2 arg2)
            => RequestRPC(method.Method.Name, bufferMode, arg1, arg2);
        public void RequestRPC<T1, T2, T3>(RpcMethod<T1, T2, T3> method, RpcBufferMode bufferMode, T1 arg1, T2 arg2, T3 arg3)
            => RequestRPC(method.Method.Name, bufferMode, arg1, arg2, arg3);
        public void RequestRPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, RpcBufferMode bufferMode, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => RequestRPC(method.Method.Name, bufferMode, arg1, arg2, arg3, arg4);
        public void RequestRPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, RpcBufferMode bufferMode, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => RequestRPC(method.Method.Name, bufferMode, arg1, arg2, arg3, arg4, arg5);
        public void RequestRPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, RpcBufferMode bufferMode, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => RequestRPC(method.Method.Name, bufferMode, arg1, arg2, arg3, arg4, arg5, arg6);

        public void RequestRPC(RpcMethod method, NetworkClient target)
            => RequestRPC(method.Method.Name, target);
        public void RequestRPC<T1>(RpcMethod<T1> method, NetworkClient target, T1 arg1)
            => RequestRPC(method.Method.Name, target, arg1);
        public void RequestRPC<T1, T2>(RpcMethod<T1, T2> method, NetworkClient target, T1 arg1, T2 arg2)
            => RequestRPC(method.Method.Name, target, arg1, arg2);
        public void RequestRPC<T1, T2, T3>(RpcMethod<T1, T2, T3> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3)
            => RequestRPC(method.Method.Name, target, arg1, arg2, arg3);
        public void RequestRPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => RequestRPC(method.Method.Name, target, arg1, arg2, arg3, arg4);
        public void RequestRPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => RequestRPC(method.Method.Name, target, arg1, arg2, arg3, arg4, arg5);
        public void RequestRPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => RequestRPC(method.Method.Name, target, arg1, arg2, arg3, arg4, arg5, arg6);

        public void RequestRPC<TResult>(RprMethod<TResult> method, NetworkClient target, RprCallback<TResult> callback)
            => RequestRPC(method.Method.Name, target, callback);
        public void RequestRPC<TResult, T1>(RprMethod<TResult, T1> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1)
            => RequestRPC(method.Method.Name, target, callback, arg1);
        public void RequestRPC<TResult, T1, T2>(RprMethod<TResult, T1, T2> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2)
            => RequestRPC(method.Method.Name, target, callback, arg1, arg2);
        public void RequestRPC<TResult, T1, T2, T3>(RprMethod<TResult, T1, T2, T3> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2, T3 arg3)
            => RequestRPC(method.Method.Name, target, callback, arg1, arg2, arg3);
        public void RequestRPC<TResult, T1, T2, T3, T4>(RprMethod<TResult, T1, T2, T3, T4> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => RequestRPC(method.Method.Name, target, callback, arg1, arg2, arg3, arg4);
        public void RequestRPC<TResult, T1, T2, T3, T4, T5>(RprMethod<TResult, T1, T2, T3, T4, T5> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => RequestRPC(method.Method.Name, target, callback, arg1, arg2, arg3, arg4, arg5);
        public void RequestRPC<TResult, T1, T2, T3, T4, T5, T6>(RprMethod<TResult, T1, T2, T3, T4, T5, T6> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => RequestRPC(method.Method.Name, target, callback, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion

        protected internal virtual void InvokeRPC(RpcCommand command)
        {
            if (FindRPC(command.Method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC with Name {command.Method} found on {GetType().Name}");

                ResolveRPC(command, RprResult.MethodNotFound);

                return;
            }

            if (ValidateAuthority(command.Sender, bind.Authority) == false)
            {
                Debug.LogWarning($"Invalid Authority To Invoke RPC {bind.Name} Sent From Client {command.Sender}");

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
                var text = $"Error trying to Parse RPC Arguments of Method '{command.Method}' on {this}, Invalid Data Sent Most Likely \n" +
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

        void ResolveRPC(RpcCommand command, RprResult result) => MNetAPI.Room.ResolveRPC(command, result);
        #endregion

        #region RPR
        void SendRPR(RpcCommand rpc, object value)
        {
            var payload = RprRequest.Write(rpc.Entity, rpc.Sender, rpc.Callback, value);

            MNetAPI.Client.Send(payload);
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

        #pragma warning disable IDE0060 // Remove unused parameter
        protected void SetSyncVar<T>(string name, T field, T value) => SetSyncVar(name, value);
        #pragma warning restore IDE0060 // Remove unused parameter

        protected void SetSyncVar(string variable, object value)
        {
            if (FindSyncVar(variable, out var bind) == false)
            {
                Debug.LogError($"No SyncVar Found With Name {variable}");
                return;
            }

            var request = bind.CreateRequest(value);

            SendSyncVar(request);

            MNetAPI.Client.Send(request);
        }

        void SendSyncVar(SyncVarRequest request)
        {
            if (IsReady == false)
            {
                Debug.LogError($"Trying to Invoke SyncVar {request.Variable} on {name} Before It's Ready, Please Wait Untill IsReady or After OnSpawn Method");
                return;
            }

            if (MNetAPI.Client.IsConnected == false)
            {
                Debug.LogWarning($"Cannot Send SyncVar {request.Variable} When Client Isn't Connected");
                return;
            }
        }

        protected internal virtual void InvokeSyncVar(SyncVarCommand command)
        {
            if(SyncVars.TryGetValue(command.Variable, out var bind) == false)
            {
                Debug.LogWarning($"No SyncVar {command.Variable} Found to Invoke");
                return;
            }

            if (ValidateAuthority(command.Sender, bind.Authority) == false)
            {
                Debug.LogWarning($"Invalid Authority To Invoke SyncVar {bind.Name} Sent From Client {command.Sender}");
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
            if (authority.HasFlag(RemoteAutority.Any)) return true;

            if (authority.HasFlag(RemoteAutority.Owner))
            {
                if (sender == Owner?.ID)
                    return true;
            }

            if (authority.HasFlag(RemoteAutority.Master))
            {
                if (sender == MNetAPI.Room.Master?.ID)
                    return true;
            }

            return false;
        }

        public NetworkBehaviour()
        {
            RPCs = new Dictionary<string, RpcBind>();

            SyncVars = new Dictionary<string, SyncVarBind>();
        }
    }
}