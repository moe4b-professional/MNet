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
        #region Broadcast
        protected void BroadcastRPC(RpcMethod method, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception);
        protected void BroadcastRPC<T1>(RpcMethod<T1> method, T1 arg1, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception, arg1);
        protected void BroadcastRPC<T1, T2>(RpcMethod<T1, T2> method, T1 arg1, T2 arg2, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception, arg1, arg2);
        protected void BroadcastRPC<T1, T2, T3>(RpcMethod<T1, T2, T3> method, T1 arg1, T2 arg2, T3 arg3, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception, arg1, arg2, arg3);
        protected void BroadcastRPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception, arg1, arg2, arg3, arg4);
        protected void BroadcastRPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception, arg1, arg2, arg3, arg4, arg5);
        protected void BroadcastRPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RemoteBufferMode buffer = RemoteBufferMode.None, NetworkClient exception = null)
            => BroadcastRPC(method.Method.Name, buffer: buffer, exception: exception, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion

        #region Target
        protected void TargetRPC(RpcMethod method, NetworkClient target)
            => TargetRPC(method.Method.Name, target.ID);
        protected void TargetRPC<T1>(RpcMethod<T1> method, NetworkClient target, T1 arg1)
            => TargetRPC(method.Method.Name, target.ID, arg1);
        protected void TargetRPC<T1, T2>(RpcMethod<T1, T2> method, NetworkClient target, T1 arg1, T2 arg2)
            => TargetRPC(method.Method.Name, target.ID, arg1, arg2);
        protected void TargetRPC<T1, T2, T3>(RpcMethod<T1, T2, T3> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3)
            => TargetRPC(method.Method.Name, target.ID, arg1, arg2, arg3);
        protected void TargetRPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => TargetRPC(method.Method.Name, target.ID, arg1, arg2, arg3, arg4);
        protected void TargetRPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => TargetRPC(method.Method.Name, target.ID, arg1, arg2, arg3, arg4, arg5);
        protected void TargetRPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => TargetRPC(method.Method.Name, target.ID, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion

        #region Query
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult>(RpcQueryMethod<TResult> method, NetworkClient target)
            => QueryRPC<TResult>(method.Method.Name, target);
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1>(RpcQueryMethod<TResult, T1> method, NetworkClient target, T1 arg1)
            => QueryRPC<TResult>(method.Method.Name, target, arg1);
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2>(RpcQueryMethod<TResult, T1, T2> method, NetworkClient target, T1 arg1, T2 arg2)
            => QueryRPC<TResult>(method.Method.Name, target, arg1, arg2);
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2, T3>(RpcQueryMethod<TResult, T1, T2, T3> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3)
            => QueryRPC<TResult>(method.Method.Name, target, arg1, arg2, arg3);
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2, T3, T4>(RpcQueryMethod<TResult, T1, T2, T3, T4> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => QueryRPC<TResult>(method.Method.Name, target, arg1, arg2, arg3, arg4);
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2, T3, T4, T5>(RpcQueryMethod<TResult, T1, T2, T3, T4, T5> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => QueryRPC<TResult>(method.Method.Name, target, arg1, arg2, arg3, arg4, arg5);
        protected UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2, T3, T4, T5, T6>(RpcQueryMethod<TResult, T1, T2, T3, T4, T5, T6> method, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => QueryRPC<TResult>(method.Method.Name, target, arg1, arg2, arg3, arg4, arg5, arg6);
        #endregion
    }
}