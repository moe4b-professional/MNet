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
    public partial class NetworkEntity
    {
        public partial class Behaviour
        {
            #region Broadcast

            #region Void
            public void BroadcastRPC(
                VoidRpcMethod rpc,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer, delivery, channel, group, exception);
            }

            public void BroadcastRPC<T1>(
                VoidRpcMethod<T1> rpc,
                T1 arg1,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer: buffer, delivery, channel, group, exception, arg1);
            }

            public void BroadcastRPC<T1, T2>(
                VoidRpcMethod<T1, T2> rpc,
                T1 arg1,
                T2 arg2,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer: buffer, delivery, channel, group, exception, arg1, arg2);
            }

            public void BroadcastRPC<T1, T2, T3>(
                VoidRpcMethod<T1, T2, T3> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer: buffer, delivery, channel, group, exception, arg1, arg2, arg3);
            }

            public void BroadcastRPC<T1, T2, T3, T4>(
                VoidRpcMethod<T1, T2, T3, T4> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer: buffer, delivery, channel, group, exception, arg1, arg2, arg3, arg4);
            }

            public void BroadcastRPC<T1, T2, T3, T4, T5>(
                VoidRpcMethod<T1, T2, T3, T4, T5> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer: buffer, delivery, channel, group, exception, arg1, arg2, arg3, arg4, arg5);
            }

            public void BroadcastRPC<T1, T2, T3, T4, T5, T6>(
                VoidRpcMethod<T1, T2, T3, T4, T5, T6> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                T6 arg6,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer: buffer, delivery, channel, group, exception, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            #endregion

            #region Return
            public void BroadcastRPC<TResult>(
                ReturnRpcMethod<TResult> rpc,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer, delivery, channel, group, exception);
            }

            public void BroadcastRPC<TResult, T1>(
                ReturnRpcMethod<TResult, T1> rpc,
                T1 arg1,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer, delivery, channel, group, exception, arg1);
            }

            public void BroadcastRPC<TResult, T1, T2>(
                ReturnRpcMethod<TResult, T1, T2> rpc,
                T1 arg1,
                T2 arg2,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer, delivery, channel, group, exception, arg1, arg2);
            }

            public void BroadcastRPC<TResult, T1, T2, T3>(
                ReturnRpcMethod<TResult, T1, T2, T3> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer, delivery, channel, group, exception, arg1, arg2, arg3);
            }

            public void BroadcastRPC<TResult, T1, T2, T3, T4>(
                ReturnRpcMethod<TResult, T1, T2, T3, T4> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer, delivery, channel, group, exception, arg1, arg2, arg3, arg4);
            }

            public void BroadcastRPC<TResult, T1, T2, T3, T4, T5>(
                ReturnRpcMethod<TResult, T1, T2, T3, T4, T5> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer, delivery, channel, group, exception, arg1, arg2, arg3, arg4, arg5);
            }

            public void BroadcastRPC<TResult, T1, T2, T3, T4, T5, T6>(
                ReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                T6 arg6,
                RemoteBufferMode buffer = RemoteBufferMode.None,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0,
                NetworkGroupID group = default,
                NetworkClient exception = null)
            {
                var name = RpcBind.GetName(rpc.Method);
                BroadcastRPC(name, buffer, delivery, channel, group, exception, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            #endregion

            #endregion

            #region Target

            #region Void
            public void TargetRPC(
                VoidRpcMethod rpc,
                NetworkClient target,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0
                )
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel);
            }

            public void TargetRPC<T1>(
                VoidRpcMethod<T1> rpc,
                NetworkClient target,
                T1 arg1,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel, arg1);
            }

            public void TargetRPC<T1, T2>(
                VoidRpcMethod<T1, T2> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel, arg1, arg2);
            }

            public void TargetRPC<T1, T2, T3>(
                VoidRpcMethod<T1, T2, T3> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel, arg1, arg2, arg3);
            }

            public void TargetRPC<T1, T2, T3, T4>(
                VoidRpcMethod<T1, T2, T3, T4> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel, arg1, arg2, arg3, arg4);
            }

            public void TargetRPC<T1, T2, T3, T4, T5>(
                VoidRpcMethod<T1, T2, T3, T4, T5> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel, arg1, arg2, arg3, arg4, arg5);
            }

            public void TargetRPC<T1, T2, T3, T4, T5, T6>(
                VoidRpcMethod<T1, T2, T3, T4, T5, T6> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                T6 arg6,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            #endregion

            #region Return
            public void TargetRPC<TResult>(
                ReturnRpcMethod<TResult> rpc,
                NetworkClient target,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel);
            }

            public void TargetRPC<TResult, T1>(
                ReturnRpcMethod<TResult, T1> rpc,
                NetworkClient target,
                T1 arg1,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel, arg1);
            }

            public void TargetRPC<TResult, T1, T2>(
                ReturnRpcMethod<TResult, T1, T2> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel, arg1, arg2);
            }

            public void TargetRPC<TResult, T1, T2, T3>(
                ReturnRpcMethod<TResult, T1, T2, T3> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel, arg1, arg2, arg3);
            }

            public void TargetRPC<TResult, T1, T2, T3, T4>(
                ReturnRpcMethod<TResult, T1, T2, T3, T4> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel, arg1, arg2, arg3, arg4);
            }

            public void TargetRPC<TResult, T1, T2, T3, T4, T5>(
                ReturnRpcMethod<TResult, T1, T2, T3, T4, T5> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel, arg1, arg2, arg3, arg4, arg5);
            }

            public void TargetRPC<TResult, T1, T2, T3, T4, T5, T6>(
                ReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                T6 arg6,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                TargetRPC(name, target, delivery, channel, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            #endregion

            #endregion

            #region Query

            #region Synchronous
            public UniTask<RprAnswer<TResult>> QueryRPC<TResult>(
                ReturnRpcMethod<TResult> rpc,
                NetworkClient target,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel);
            }

            public UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1>(
                ReturnRpcMethod<TResult, T1> rpc,
                NetworkClient target,
                T1 arg1,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel, arg1);
            }

            public UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2>(
                ReturnRpcMethod<TResult, T1, T2> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel, arg1, arg2);
            }

            public UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2, T3>(
                ReturnRpcMethod<TResult, T1, T2, T3> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel, arg1, arg2, arg3);
            }

            public UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2, T3, T4>(
                ReturnRpcMethod<TResult, T1, T2, T3, T4> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel, arg1, arg2, arg3, arg4);
            }

            public UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2, T3, T4, T5>(
                ReturnRpcMethod<TResult, T1, T2, T3, T4, T5> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel, arg1, arg2, arg3, arg4, arg5);
            }

            public UniTask<RprAnswer<TResult>> QueryRPC<TResult, T1, T2, T3, T4, T5, T6>(
                ReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                T6 arg6,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            #endregion

            #region Asynchronous
            public UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult>(
                AsyncReturnRpcMethod<TResult> rpc,
                NetworkClient target,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel);
            }

            public UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult, T1>(
                AsyncReturnRpcMethod<TResult, T1> rpc,
                NetworkClient target,
                T1 arg1,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel, arg1);
            }

            public UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult, T1, T2>(
                AsyncReturnRpcMethod<TResult, T1, T2> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel, arg1, arg2);
            }

            public UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult, T1, T2, T3>(
                AsyncReturnRpcMethod<TResult, T1, T2, T3> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel, arg1, arg2, arg3);
            }

            public UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult, T1, T2, T3, T4>(
                AsyncReturnRpcMethod<TResult, T1, T2, T3, T4> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel, arg1, arg2, arg3, arg4);
            }

            public UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult, T1, T2, T3, T4, T5>(
                AsyncReturnRpcMethod<TResult, T1, T2, T3, T4, T5> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel, arg1, arg2, arg3, arg4, arg5);
            }

            public UniTask<RprAnswer<TResult>> QueryAsyncRPC<TResult, T1, T2, T3, T4, T5, T6>(
                AsyncReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6> rpc,
                NetworkClient target,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                T6 arg6,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                return QueryRPC<TResult>(name, target, delivery, channel, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            #endregion

            #endregion

            #region Buffer

            #region Void
            public void BufferRPC(
                VoidRpcMethod rpc,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer, delivery, channel);
            }

            public void BufferRPC<T1>(
                VoidRpcMethod<T1> rpc,
                T1 arg1,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer: buffer, delivery, channel, arg1);
            }

            public void BufferRPC<T1, T2>(
                VoidRpcMethod<T1, T2> rpc,
                T1 arg1,
                T2 arg2,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer: buffer, delivery, channel, arg1, arg2);
            }

            public void BufferRPC<T1, T2, T3>(
                VoidRpcMethod<T1, T2, T3> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer: buffer, delivery, channel, arg1, arg2, arg3);
            }

            public void BufferRPC<T1, T2, T3, T4>(
                VoidRpcMethod<T1, T2, T3, T4> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer: buffer, delivery, channel, arg1, arg2, arg3, arg4);
            }

            public void BufferRPC<T1, T2, T3, T4, T5>(
                VoidRpcMethod<T1, T2, T3, T4, T5> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer: buffer, delivery, channel, arg1, arg2, arg3, arg4, arg5);
            }

            public void BufferRPC<T1, T2, T3, T4, T5, T6>(
                VoidRpcMethod<T1, T2, T3, T4, T5, T6> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                T6 arg6,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer: buffer, delivery, channel, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            #endregion

            #region Return
            public void BufferRPC<TResult>(
                ReturnRpcMethod<TResult> rpc,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer, delivery, channel);
            }

            public void BufferRPC<TResult, T1>(
                ReturnRpcMethod<TResult, T1> rpc,
                T1 arg1,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer, delivery, channel, arg1);
            }

            public void BufferRPC<TResult, T1, T2>(
                ReturnRpcMethod<TResult, T1, T2> rpc,
                T1 arg1,
                T2 arg2,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer, delivery, channel, arg1, arg2);
            }

            public void BufferRPC<TResult, T1, T2, T3>(
                ReturnRpcMethod<TResult, T1, T2, T3> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer, delivery, channel, arg1, arg2, arg3);
            }

            public void BufferRPC<TResult, T1, T2, T3, T4>(
                ReturnRpcMethod<TResult, T1, T2, T3, T4> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer, delivery, channel, arg1, arg2, arg3, arg4);
            }

            public void BufferRPC<TResult, T1, T2, T3, T4, T5>(
                ReturnRpcMethod<TResult, T1, T2, T3, T4, T5> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer, delivery, channel, arg1, arg2, arg3, arg4, arg5);
            }

            public void BufferRPC<TResult, T1, T2, T3, T4, T5, T6>(
                ReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6> rpc,
                T1 arg1,
                T2 arg2,
                T3 arg3,
                T4 arg4,
                T5 arg5,
                T6 arg6,
                RemoteBufferMode buffer = RemoteBufferMode.Last,
                DeliveryMode delivery = DeliveryMode.ReliableOrdered,
                byte channel = 0)
            {
                var name = RpcBind.GetName(rpc.Method);
                BufferRPC(name, buffer, delivery, channel, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            #endregion

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
}