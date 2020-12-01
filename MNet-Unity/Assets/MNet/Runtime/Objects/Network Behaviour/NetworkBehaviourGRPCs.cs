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

namespace MNet
{
    public partial class NetworkBehaviour
    {
        #region Broadcast
        public void BroadcastRPC(RpcMethod method, RpcBufferMode buffer = RpcBufferMode.None, NetworkClientID? exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception);
        public void BroadcastRPC<T1>(RpcMethod<T1> method, T1 arg1, RpcBufferMode buffer = RpcBufferMode.None, NetworkClientID? exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception, arg1);
        public void BroadcastRPC<T1, T2>(RpcMethod<T1, T2> method, T1 arg1, T2 arg2, RpcBufferMode buffer = RpcBufferMode.None, NetworkClientID? exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception, arg1, arg2);
        public void BroadcastRPC<T1, T2, T3>(RpcMethod<T1, T2, T3> method, T1 arg1, T2 arg2, T3 arg3, RpcBufferMode buffer = RpcBufferMode.None, NetworkClientID? exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception, arg1, arg2, arg3);
        public void BroadcastRPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, RpcBufferMode buffer = RpcBufferMode.None, NetworkClientID? exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception, arg1, arg2, arg3, arg4);
        public void BroadcastRPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RpcBufferMode buffer = RpcBufferMode.None, NetworkClientID? exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception, arg1, arg2, arg3, arg4, arg5);
        public void BroadcastRPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RpcBufferMode buffer = RpcBufferMode.None, NetworkClientID? exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion

        #region Targeted
        public void TargetRPC(RpcMethod method, NetworkClient target)
            => TargetRPC(method.Method.Name, target);
        public void TargetRPC<T1>(RpcMethod<T1> method, NetworkClient target, T1 arg1)
            => TargetRPC(method.Method.Name, target, arg1);
        public void TargetRPC<T1, T2>(RpcMethod<T1, T2> method, NetworkClient target, T1 arg1, T2 arg2)
            => TargetRPC(method.Method.Name, target, arg1, arg2);
        public void TargetRPC<T1, T2, T3>(RpcMethod<T1, T2, T3> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3)
            => TargetRPC(method.Method.Name, target, arg1, arg2, arg3);
        public void TargetRPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => TargetRPC(method.Method.Name, target, arg1, arg2, arg3, arg4);
        public void TargetRPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => TargetRPC(method.Method.Name, target, arg1, arg2, arg3, arg4, arg5);
        public void TargetRPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => TargetRPC(method.Method.Name, target, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion

        #region RPR
        public void ReturnRPC<TResult>(RprMethod<TResult> method, NetworkClient target, RprCallback<TResult> callback)
            => ReturnRPC(method.Method.Name, target, callback);
        public void ReturnRPC<TResult, T1>(RprMethod<TResult, T1> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1)
            => ReturnRPC(method.Method.Name, target, callback, arg1);
        public void ReturnRPC<TResult, T1, T2>(RprMethod<TResult, T1, T2> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2)
            => ReturnRPC(method.Method.Name, target, callback, arg1, arg2);
        public void ReturnRPC<TResult, T1, T2, T3>(RprMethod<TResult, T1, T2, T3> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2, T3 arg3)
            => ReturnRPC(method.Method.Name, target, callback, arg1, arg2, arg3);
        public void ReturnRPC<TResult, T1, T2, T3, T4>(RprMethod<TResult, T1, T2, T3, T4> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => ReturnRPC(method.Method.Name, target, callback, arg1, arg2, arg3, arg4);
        public void ReturnRPC<TResult, T1, T2, T3, T4, T5>(RprMethod<TResult, T1, T2, T3, T4, T5> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => ReturnRPC(method.Method.Name, target, callback, arg1, arg2, arg3, arg4, arg5);
        public void ReturnRPC<TResult, T1, T2, T3, T4, T5, T6>(RprMethod<TResult, T1, T2, T3, T4, T5, T6> method, NetworkClient target, RprCallback<TResult> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => ReturnRPC(method.Method.Name, target, callback, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion
    }
}