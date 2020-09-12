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

namespace Backend
{
    [RequireComponent(typeof(NetworkEntity))]
    public partial class NetworkBehaviour : MonoBehaviour
	{
        public NetworkBehaviourID ID { get; protected set; }

        public NetworkEntity Entity { get; protected set; }
        public bool IsMine => Entity == null ? false : Entity.IsMine;
        public bool IsReady => Entity == null ? false : Entity.IsReady;
        public bool IsSceneObject => Entity == null ? false : Entity.IsSceneObject;

        public NetworkClient Owner => Entity?.Owner;

        public AttributesCollection Attributes => Entity?.Attributes;

        protected virtual void Awake()
        {
            if (IsReady == false) enabled = false;
        }

        protected virtual void Start()
        {

        }

        public void Configure(NetworkEntity entity, NetworkBehaviourID id)
        {
            Entity = entity;

            this.ID = id;

            RPCs = new RpcBindCollection(this);

            enabled = true;

            OnSpawn();
        }

        protected virtual void OnSpawn() { }

        #region RPC
        public RpcBindCollection RPCs { get; protected set; }

        protected void RequestRPC(string method, params object[] arguments) => RequestRPC(method, RpcBufferMode.None, arguments);
        protected void RequestRPC(string method, RpcBufferMode bufferMode, params object[] arguments)
        {
            if (RPCs.Find(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return;
            }

            var payload = bind.CreateRequest(bufferMode, arguments);

            RequestRPC(payload);
        }

        protected void RequestRPC(string method, NetworkClient target, params object[] arguments)
        {
            if (RPCs.Find(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return;
            }

            var payload = bind.CreateRequest(target.ID, arguments);

            RequestRPC(payload);
        }

        protected void RequestRPC<TResult>(string method, NetworkClient target, Action<TResult> result, params object[] arguments)
        {
            if (RPCs.Find(method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC Found With Name {method}");
                return;
            }

            var callback = Entity.RegisterRpcCallback(result.Method, result.Target);

            var payload = bind.CreateRequest(target.ID, callback.ID, arguments);

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

        public void RequestRPC<TResult>(RpcCallbackMethod<TResult> method, NetworkClient target, Action<TResult> callback)
            => RequestRPC(method.Method.Name, target, callback);

        public void RequestRPC<TResult, T1>(RpcCallbackMethod<TResult, T1> method, NetworkClient target, Action<TResult> callback, T1 arg1)
            => RequestRPC(method.Method.Name, target, callback, arg1);
        #endregion

        public void InvokeRPC(RpcCommand command)
        {
            if(RPCs.Find(command.Method, out var bind) == false)
            {
                Debug.LogWarning($"No RPC with Name {command.Method} found on {GetType().Name}");
                return;
            }

            if (ValidateRpcAuthority(command, bind) == false)
            {
                Debug.LogWarning($"Invalid Authority To Invoke RPC {bind.ID} Sent From Client {command.Sender}");
                return;
            }

            var result = bind.Invoke(command);

            if (command.Type == RpcType.Callback) CallbackRPC(command, result);
        }

        void CallbackRPC(RpcCommand command, object result)
        {
            var payload = RpcCallback.Write(command.Entity, command.Callback, result);

            NetworkAPI.Client.Send(payload);
        }

        public bool ValidateRpcAuthority(RpcCommand command, RpcBind bind)
        {
            if (bind.Authority == RpcAuthority.Any) return true;

            if (bind.Authority == RpcAuthority.Owner) return command.Sender == Owner.ID;

            if (bind.Authority == RpcAuthority.Master) return command.Sender == NetworkAPI.Room.Master?.ID;

            return true;
        }
        #endregion
    }
}