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
        #region Default
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
        #endregion

        #region Buffered
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
        #endregion

        #region Targeted
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
        #endregion

        #region RPR
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
    }
}