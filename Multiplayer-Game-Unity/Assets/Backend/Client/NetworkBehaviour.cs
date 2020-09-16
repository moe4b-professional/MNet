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
using Game;
using System.Threading;

namespace Backend
{
    public partial class NetworkBehaviour : MonoBehaviour
	{
        public NetworkBehaviourID ID { get; protected set; }

        public NetworkEntity Entity { get; protected set; }

        public bool IsMine => Entity == null ? false : Entity.IsMine;
        public bool IsReady => Entity == null ? false : Entity.IsReady;

        public NetworkClient Owner => Entity?.Owner;

        public AttributesCollection Attributes => Entity?.Attributes;

        protected virtual void Awake()
        {
            ParseRPCs();
            ParseSyncVars();
        }

        public void Configure(NetworkEntity entity, NetworkBehaviourID id)
        {
            this.Entity = entity;
            this.ID = id;

            entity.OnSpawn += SpawnCallback;
        }

        void SpawnCallback()
        {
            enabled = true;

            OnSpawn();
        }
        protected virtual void OnSpawn() { }

        protected virtual void Start()
        {

        }

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

                if (RPCs.ContainsKey(bind.ID))
                    throw new Exception($"Rpc Named {bind.ID} Already Registered On Behaviour {GetType()}, Please Assign Every RPC a Unique Name And Don't Overload the RPC Methods");

                RPCs.Add(bind.ID, bind);
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

            RequestRPC(payload);
        }

        protected void RequestRPC(string method, NetworkClient target, params object[] arguments)
        {
            if (FindRPC(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return;
            }

            var payload = bind.CreateRequest(target.ID, arguments);

            RequestRPC(payload);
        }

        protected void RequestRPC<TResult>(string method, NetworkClient target, RprMethod<TResult> result, params object[] arguments)
        {
            if (FindRPC(method, out var bind) == false)
            {
                Debug.LogError($"No RPC Found With Name {method}");
                return;
            }

            var rpr = Entity.RegisterRPR(result.Method, result.Target);

            var payload = bind.CreateRequest(target.ID, rpr.ID, arguments);

            RequestRPC(payload);
        }

        protected virtual void RequestRPC(RpcRequest request)
        {
            if (IsReady == false)
            {
                Debug.LogError($"Trying to Invoke RPC {request.Method} on {name} Before It's Ready, Please Wait Untill IsReady or After OnSpawn Method");
                return;
            }

            if (NetworkAPI.Client.IsConnected == false)
            {
                Debug.LogWarning($"Cannot Send RPC {request.Method} When Client Isn't Connected");
                return;
            }

            NetworkAPI.Client.Send(request);
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

        public void RequestRPC<TResult>(RpcReturnMethod<TResult> method, NetworkClient target, RprMethod<TResult> callback)
            => RequestRPC(method.Method.Name, target, callback);
        public void RequestRPC<TResult, T1>(RpcReturnMethod<TResult, T1> method, NetworkClient target, RprMethod<TResult> callback, T1 arg1)
            => RequestRPC(method.Method.Name, target, callback, arg1);
        public void RequestRPC<TResult, T1, T2>(RpcReturnMethod<TResult, T1, T2> method, NetworkClient target, RprMethod<TResult> callback, T1 arg1, T2 arg2)
            => RequestRPC(method.Method.Name, target, callback, arg1, arg2);
        public void RequestRPC<TResult, T1, T2, T3>(RpcReturnMethod<TResult, T1, T2, T3> method, NetworkClient target, RprMethod<TResult> callback, T1 arg1, T2 arg2, T3 arg3)
            => RequestRPC(method.Method.Name, target, callback, arg1, arg2, arg3);
        public void RequestRPC<TResult, T1, T2, T3, T4>(RpcReturnMethod<TResult, T1, T2, T3, T4> method, NetworkClient target, RprMethod<TResult> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => RequestRPC(method.Method.Name, target, callback, arg1, arg2, arg3, arg4);
        public void RequestRPC<TResult, T1, T2, T3, T4, T5>(RpcReturnMethod<TResult, T1, T2, T3, T4, T5> method, NetworkClient target, RprMethod<TResult> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => RequestRPC(method.Method.Name, target, callback, arg1, arg2, arg3, arg4, arg5);
        public void RequestRPC<TResult, T1, T2, T3, T4, T5, T6>(RpcReturnMethod<TResult, T1, T2, T3, T4, T5, T6> method, NetworkClient target, RprMethod<TResult> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => RequestRPC(method.Method.Name, target, callback, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion

        public void InvokeRPC(RpcCommand command)
        {
            if (FindRPC(command.Method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC with Name {command.Method} found on {GetType().Name}");

                if (command.Type == RpcType.Return) SendRPR(command, RprResult.MethodNotFound);

                return;
            }

            if (ValidateAuthority(command.Sender, bind.Authority) == false)
            {
                Debug.LogWarning($"Invalid Authority To Invoke RPC {bind.ID} Sent From Client {command.Sender}");

                if (command.Type == RpcType.Return) SendRPR(command, RprResult.InvalidAuthority);

                return;
            }

            object[] arguments;
            RpcInfo info;
            try
            {
                arguments = bind.ParseArguments(command, out info);
            }
            catch (Exception e)
            {
                var text = $"Error trying to Parse RPC Arguments of Method '{command.Method}' on {this}, Invalid Data Sent Most Likely \n" +
                    $"Exception: \n" +
                    $"{e.ToString()}";

                Debug.LogWarning(text, this);

                if (command.Type == RpcType.Return) SendRPR(command, RprResult.InvalidArguments);

                return;
            }

            object result;
            try
            {
                result = bind.Invoke(arguments);
            }
            catch (TargetInvocationException)
            {
                if (command.Type == RpcType.Return) SendRPR(command, RprResult.RuntimeException);
                throw;
            }
            catch (Exception)
            {
                var text = $"Error Trying to Invoke RPC Method '{command.Method}' on '{this}', " +
                    $"Please Ensure Method is Implemented And Invoked Correctly";

                Debug.LogWarning(text, this);

                if (command.Type == RpcType.Return) SendRPR(command, RprResult.RuntimeException);

                return;
            }

            if (command.Type == RpcType.Return) SendRPR(command, result);
        }
        #endregion

        #region RPR
        void SendRPR(RpcCommand rpc, RprResult result)
        {
            var payload = RprRequest.Write(rpc.Entity, rpc.Sender, rpc.Callback, result);

            NetworkAPI.Client.Send(payload);
        }

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

                if (SyncVars.ContainsKey(bind.ID))
                    throw new Exception($"SyncVar Named {bind.ID} Already Registered On Behaviour {type}, Please Assign Every SyncVar a Unique Name");

                SyncVars.Add(bind.ID, bind);
            }

            foreach (var property in type.GetProperties(flags))
            {
                var attribute = property.GetCustomAttribute<SyncVarAttribute>();

                if (attribute == null) continue;

                var bind = new SyncVarBind(this, attribute, property);

                if (SyncVars.ContainsKey(bind.ID))
                    throw new Exception($"SyncVar Named {bind.ID} Already Registered On Behaviour {type}, Please Assign Every SyncVar a Unique Name");

                SyncVars.Add(bind.ID, bind);
            }
        }

        bool FindSyncVar(string variable, out SyncVarBind bind) => SyncVars.TryGetValue(variable, out bind);

        protected void UpdateSyncVar(string variable, object value)
        {
            if (FindSyncVar(variable, out var bind) == false)
            {
                Debug.LogError($"No SyncVar Found With Name {variable}");
                return;
            }

            var request = bind.CreateRequest(value);

            SendSyncVar(request);

            NetworkAPI.Client.Send(request);
        }

        void SendSyncVar(SyncVarRequest request)
        {
            if (IsReady == false)
            {
                Debug.LogError($"Trying to Invoke SyncVar {request.Variable} on {name} Before It's Ready, Please Wait Untill IsReady or After OnSpawn Method");
                return;
            }

            if (NetworkAPI.Client.IsConnected == false)
            {
                Debug.LogWarning($"Cannot Send SyncVar {request.Variable} When Client Isn't Connected");
                return;
            }
        }

        internal void InvokeSyncVar(SyncVarCommand command)
        {
            if(SyncVars.TryGetValue(command.Variable, out var bind) == false)
            {
                Debug.LogWarning($"No SyncVar {command.Variable} Found to Invoke");
                return;
            }

            if (ValidateAuthority(command.Sender, bind.Authority) == false)
            {
                Debug.LogWarning($"Invalid Authority To Invoke SyncVar {bind.ID} Sent From Client {command.Sender}");
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
                    $"{ex.ToString()}";

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

        public bool ValidateAuthority(NetworkClientID sender, EntityAuthorityType authority)
        {
            if (authority == EntityAuthorityType.Any) return true;

            if (authority == EntityAuthorityType.Owner) return sender == Owner?.ID;

            if (authority == EntityAuthorityType.Master) return sender == NetworkAPI.Room.Master?.ID;

            return false;
        }

        public NetworkBehaviour()
        {
            RPCs = new Dictionary<string, RpcBind>();

            SyncVars = new Dictionary<string, SyncVarBind>();
        }
    }
}