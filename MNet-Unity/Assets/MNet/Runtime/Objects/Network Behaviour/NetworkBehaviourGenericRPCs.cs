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

using Cysharp.Threading.Tasks;

namespace MNet
{
    public partial class NetworkBehaviour
    {
        #region Broadcast Void
        protected void BroadcastRPC(VoidRpcMethod rpc, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception);
        protected void BroadcastRPC<T1>(VoidRpcMethod<T1> rpc, T1 arg1, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception, arg1);
        protected void BroadcastRPC<T1, T2>(VoidRpcMethod<T1, T2> rpc, T1 arg1, T2 arg2, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception, arg1, arg2);
        protected void BroadcastRPC<T1, T2, T3>(VoidRpcMethod<T1, T2, T3> rpc, T1 arg1, T2 arg2, T3 arg3, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception, arg1, arg2, arg3);
        protected void BroadcastRPC<T1, T2, T3, T4>(VoidRpcMethod<T1, T2, T3, T4> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception, arg1, arg2, arg3, arg4);
        protected void BroadcastRPC<T1, T2, T3, T4, T5>(VoidRpcMethod<T1, T2, T3, T4, T5> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception, arg1, arg2, arg3, arg4, arg5);
        protected void BroadcastRPC<T1, T2, T3, T4, T5, T6>(VoidRpcMethod<T1, T2, T3, T4, T5, T6> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion

        #region Broadcast Return
        protected void BroadcastRPC<TResult>(ReturnRpcMethod<TResult> rpc, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception);
        protected void BroadcastRPC<TResult, T1>(ReturnRpcMethod<TResult, T1> rpc, T1 arg1, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception, arg1);
        protected void BroadcastRPC<TResult, T1, T2>(ReturnRpcMethod<TResult, T1, T2> rpc, T1 arg1, T2 arg2, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception, arg1, arg2);
        protected void BroadcastRPC<TResult, T1, T2, T3>(ReturnRpcMethod<TResult, T1, T2, T3> rpc, T1 arg1, T2 arg2, T3 arg3, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception, arg1, arg2, arg3);
        protected void BroadcastRPC<TResult, T1, T2, T3, T4>(ReturnRpcMethod<TResult, T1, T2, T3, T4> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception, arg1, arg2, arg3, arg4);
        protected void BroadcastRPC<TResult, T1, T2, T3, T4, T5>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception, arg1, arg2, arg3, arg4, arg5);
        protected void BroadcastRPC<TResult, T1, T2, T3, T4, T5, T6>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(rpc.Method, buffer: buffer, exception: exception, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion

        #region Target Void
        protected void TargetRPC(VoidRpcMethod rpc, NetworkClient target)
            => TargetRPC(rpc.Method, target);
        protected void TargetRPC<T1>(VoidRpcMethod<T1> rpc, NetworkClient target, T1 arg1)
            => TargetRPC(rpc.Method, target, arg1);
        protected void TargetRPC<T1, T2>(VoidRpcMethod<T1, T2> rpc, NetworkClient target, T1 arg1, T2 arg2)
            => TargetRPC(rpc.Method, target, arg1, arg2);
        protected void TargetRPC<T1, T2, T3>(VoidRpcMethod<T1, T2, T3> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3)
            => TargetRPC(rpc.Method, target, arg1, arg2, arg3);
        protected void TargetRPC<T1, T2, T3, T4>(VoidRpcMethod<T1, T2, T3, T4> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => TargetRPC(rpc.Method, target, arg1, arg2, arg3, arg4);
        protected void TargetRPC<T1, T2, T3, T4, T5>(VoidRpcMethod<T1, T2, T3, T4, T5> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => TargetRPC(rpc.Method, target, arg1, arg2, arg3, arg4, arg5);
        protected void TargetRPC<T1, T2, T3, T4, T5, T6>(VoidRpcMethod<T1, T2, T3, T4, T5, T6> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => TargetRPC(rpc.Method, target, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion

        #region Target Return
        protected void TargetRPC<TResult>(ReturnRpcMethod<TResult> rpc, NetworkClient target)
            => TargetRPC(rpc.Method, target);
        protected void TargetRPC<TResult, T1>(ReturnRpcMethod<TResult, T1> rpc, NetworkClient target, T1 arg1)
            => TargetRPC(rpc.Method, target, arg1);
        protected void TargetRPC<TResult, T1, T2>(ReturnRpcMethod<TResult, T1, T2> rpc, NetworkClient target, T1 arg1, T2 arg2)
            => TargetRPC(rpc.Method, target, arg1, arg2);
        protected void TargetRPC<TResult, T1, T2, T3>(ReturnRpcMethod<TResult, T1, T2, T3> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3)
            => TargetRPC(rpc.Method, target, arg1, arg2, arg3);
        protected void TargetRPC<TResult, T1, T2, T3, T4>(ReturnRpcMethod<TResult, T1, T2, T3, T4> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => TargetRPC(rpc.Method, target, arg1, arg2, arg3, arg4);
        protected void TargetRPC<TResult, T1, T2, T3, T4, T5>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => TargetRPC(rpc.Method, target, arg1, arg2, arg3, arg4, arg5);
        protected void TargetRPC<TResult, T1, T2, T3, T4, T5, T6>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => TargetRPC(rpc.Method, target, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion

        #region Query Synchronous
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult>(ReturnRpcMethod<TResult> rpc, NetworkClient target)
            => QueryRPC<TResult>(rpc.Method, target);
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1>(ReturnRpcMethod<TResult, T1> rpc, NetworkClient target, T1 arg1)
            => QueryRPC<TResult>(rpc.Method, target, arg1);
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2>(ReturnRpcMethod<TResult, T1, T2> rpc, NetworkClient target, T1 arg1, T2 arg2)
            => QueryRPC<TResult>(rpc.Method, target, arg1, arg2);
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2, T3>(ReturnRpcMethod<TResult, T1, T2, T3> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3)
            => QueryRPC<TResult>(rpc.Method, target, arg1, arg2, arg3);
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2, T3, T4>(ReturnRpcMethod<TResult, T1, T2, T3, T4> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => QueryRPC<TResult>(rpc.Method, target, arg1, arg2, arg3, arg4);
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2, T3, T4, T5>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => QueryRPC<TResult>(rpc.Method, target, arg1, arg2, arg3, arg4, arg5);
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2, T3, T4, T5, T6>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => QueryRPC<TResult>(rpc.Method, target, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion

        #region Query Asynchronous
        protected UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult>(AsyncReturnRpcMethod<TResult> rpc, NetworkClient target)
            => QueryRPC<TResult>(rpc.Method, target);
        protected UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult, T1>(AsyncReturnRpcMethod<TResult, T1> rpc, NetworkClient target, T1 arg1)
            => QueryRPC<TResult>(rpc.Method, target, arg1);
        protected UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult, T1, T2>(AsyncReturnRpcMethod<TResult, T1, T2> rpc, NetworkClient target, T1 arg1, T2 arg2)
            => QueryRPC<TResult>(rpc.Method, target, arg1, arg2);
        protected UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult, T1, T2, T3>(AsyncReturnRpcMethod<TResult, T1, T2, T3> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3)
            => QueryRPC<TResult>(rpc.Method, target, arg1, arg2, arg3);
        protected UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult, T1, T2, T3, T4>(AsyncReturnRpcMethod<TResult, T1, T2, T3, T4> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => QueryRPC<TResult>(rpc.Method, target, arg1, arg2, arg3, arg4);
        protected UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult, T1, T2, T3, T4, T5>(AsyncReturnRpcMethod<TResult, T1, T2, T3, T4, T5> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => QueryRPC<TResult>(rpc.Method, target, arg1, arg2, arg3, arg4, arg5);
        protected UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult, T1, T2, T3, T4, T5, T6>(AsyncReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => QueryRPC<TResult>(rpc.Method, target, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion

        #region Delegates
        public delegate void VoidRpcMethod(RpcInfo info);
        public delegate void VoidRpcMethod<T1>(T1 arg1, RpcInfo info);
        public delegate void VoidRpcMethod<T1, T2>(T1 arg1, T2 arg2, RpcInfo info);
        public delegate void VoidRpcMethod<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, RpcInfo info);
        public delegate void VoidRpcMethod<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, RpcInfo info);
        public delegate void VoidRpcMethod<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RpcInfo info);
        public delegate void VoidRpcMethod<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RpcInfo info);

        public delegate TResult ReturnRpcMethod<TResult>(RpcInfo info);
        public delegate TResult ReturnRpcMethod<TResult, T1>(T1 arg1, RpcInfo info);
        public delegate TResult ReturnRpcMethod<TResult, T1, T2>(T1 arg1, T2 arg2, RpcInfo info);
        public delegate TResult ReturnRpcMethod<TResult, T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, RpcInfo info);
        public delegate TResult ReturnRpcMethod<TResult, T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, RpcInfo info);
        public delegate TResult ReturnRpcMethod<TResult, T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RpcInfo info);
        public delegate TResult ReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RpcInfo info);

        public delegate UniTask<TResult> AsyncReturnRpcMethod<TResult>(RpcInfo info);
        public delegate UniTask<TResult> AsyncReturnRpcMethod<TResult, T1>(T1 arg1, RpcInfo info);
        public delegate UniTask<TResult> AsyncReturnRpcMethod<TResult, T1, T2>(T1 arg1, T2 arg2, RpcInfo info);
        public delegate UniTask<TResult> AsyncReturnRpcMethod<TResult, T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, RpcInfo info);
        public delegate UniTask<TResult> AsyncReturnRpcMethod<TResult, T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, RpcInfo info);
        public delegate UniTask<TResult> AsyncReturnRpcMethod<TResult, T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RpcInfo info);
        public delegate UniTask<TResult> AsyncReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RpcInfo info);
        #endregion
    }
}