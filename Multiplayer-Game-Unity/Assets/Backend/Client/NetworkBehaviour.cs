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

namespace Backend
{
    [RequireComponent(typeof(NetworkEntity))]
    public partial class NetworkBehaviour : MonoBehaviour
	{
        public NetworkBehaviourID ID { get; protected set; }

        public NetworkEntity Entity { get; protected set; }

        public AttributesCollection Attributes => Entity?.Attributes;

        public void Configure(NetworkEntity entity, NetworkBehaviourID id)
        {
            Entity = entity;

            this.ID = id;

            RPCs = new RpcCollection(this);

            OnSpawn();
        }

        protected virtual void OnSpawn() { }

        public bool IsMine => Entity.IsMine;

        public RpcCollection RPCs { get; protected set; }
        
        protected void RequestRPC(string method, params object[] arguments) => RequestRPC(method, RpcBufferMode.None, arguments);
        protected void RequestRPC(string method, RpcBufferMode bufferMode, params object[] arguments)
        {
            if (RPCs.Find(method, out var bind))
            {
                var payload = bind.CreateRequest(bufferMode, arguments);

                SendRpc(payload);
            }
            else
                Debug.LogWarning($"No RPC Found With Name {method}");
        }

        protected void RequestRPC(string method, NetworkClient client, params object[] arguments)
        {
            if (RPCs.Find(method, out var bind))
            {
                var payload = bind.CreateRequest(client.ID, arguments);

                SendRpc(payload);
            }
            else
                Debug.LogWarning($"No RPC Found With Name {method}");
        }

        #region Generic RPC Methods
        public void RequestRPC(RpcMethod callback)
            => RequestRPC(callback.Method.Name);
        public void RequestRPC<T1>(RpcMethod<T1> callback, T1 arg1)
            => RequestRPC(callback.Method.Name, arg1);
        public void RequestRPC<T1, T2>(RpcMethod<T1, T2> callback, T1 arg1, T2 arg2)
            => RequestRPC(callback.Method.Name, arg1, arg2);
        public void RequestRPC<T1, T2, T3>(RpcMethod<T1, T2, T3> callback, T1 arg1, T2 arg2, T3 arg3)
            => RequestRPC(callback.Method.Name, arg1, arg2, arg3);
        public void RequestRPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => RequestRPC(callback.Method.Name, arg1, arg2, arg3, arg4);
        public void RequestRPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => RequestRPC(callback.Method.Name, arg1, arg2, arg3, arg4, arg5);
        public void RequestRPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => RequestRPC(callback.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6);

        public void RequestRPC(RpcMethod callback, RpcBufferMode bufferMode)
            => RequestRPC(callback.Method.Name, bufferMode);
        public void RequestRPC<T1>(RpcMethod<T1> callback, RpcBufferMode bufferMode, T1 arg1)
            => RequestRPC(callback.Method.Name, bufferMode, arg1);
        public void RequestRPC<T1, T2>(RpcMethod<T1, T2> callback, RpcBufferMode bufferMode, T1 arg1, T2 arg2)
            => RequestRPC(callback.Method.Name, bufferMode, arg1, arg2);
        public void RequestRPC<T1, T2, T3>(RpcMethod<T1, T2, T3> callback, RpcBufferMode bufferMode, T1 arg1, T2 arg2, T3 arg3)
            => RequestRPC(callback.Method.Name, bufferMode, arg1, arg2, arg3);
        public void RequestRPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> callback, RpcBufferMode bufferMode, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => RequestRPC(callback.Method.Name, bufferMode, arg1, arg2, arg3, arg4);
        public void RequestRPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> callback, RpcBufferMode bufferMode, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => RequestRPC(callback.Method.Name, bufferMode, arg1, arg2, arg3, arg4, arg5);
        public void RequestRPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> callback, RpcBufferMode bufferMode, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => RequestRPC(callback.Method.Name, bufferMode, arg1, arg2, arg3, arg4, arg5, arg6);

        public void RequestRPC(RpcMethod callback, NetworkClient target)
            => RequestRPC(callback.Method.Name, target);
        public void RequestRPC<T1>(RpcMethod<T1> callback, NetworkClient target, T1 arg1)
            => RequestRPC(callback.Method.Name, target, arg1);
        public void RequestRPC<T1, T2>(RpcMethod<T1, T2> callback, NetworkClient target, T1 arg1, T2 arg2)
            => RequestRPC(callback.Method.Name, target, arg1, arg2);
        public void RequestRPC<T1, T2, T3>(RpcMethod<T1, T2, T3> callback, NetworkClient target, T1 arg1, T2 arg2, T3 arg3)
            => RequestRPC(callback.Method.Name, target, arg1, arg2, arg3);
        public void RequestRPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> callback, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => RequestRPC(callback.Method.Name, target, arg1, arg2, arg3, arg4);
        public void RequestRPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> callback, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => RequestRPC(callback.Method.Name, target, arg1, arg2, arg3, arg4, arg5);
        public void RequestRPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> callback, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => RequestRPC(callback.Method.Name, target, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion

        protected void SendRpc<T>(T request)
            where T : RpcRequest
        {
            if (NetworkAPI.Client.IsConnected == false)
            {
                Debug.LogWarning($"Cannot Send RPC {request.Method} When Client Isn't Connected");
                return;
            }

            NetworkAPI.Client.Send(request);
        }

        public void InvokeRpc(RpcCommand command)
        {
            if(RPCs.Find(command.Method, out var bind))
                bind.InvokeCommand(command);
            else
                Debug.LogWarning($"No RPC with Name {command.Method} found on {GetType().Name}");
        }
    }
}