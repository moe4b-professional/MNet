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
            public BroadcastRpcPacket BroadcastRPC(VoidRpcMethod rpc)
            {
                var name = RpcBind.GetName(rpc.Method);
                return BroadcastRPC(name, default);
            }

            public BroadcastRpcPacket BroadcastRPC<T1>(VoidRpcMethod<T1> rpc, T1 arg1)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);

                return BroadcastRPC(name, writer);
            }

            public BroadcastRpcPacket BroadcastRPC<T1, T2>(VoidRpcMethod<T1, T2> rpc, T1 arg1, T2 arg2)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);

                return BroadcastRPC(name, writer);
            }

            public BroadcastRpcPacket BroadcastRPC<T1, T2, T3>(VoidRpcMethod<T1, T2, T3> rpc, T1 arg1, T2 arg2, T3 arg3)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);

                return BroadcastRPC(name, writer);
            }

            public BroadcastRpcPacket BroadcastRPC<T1, T2, T3, T4>(VoidRpcMethod<T1, T2, T3, T4> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);

                return BroadcastRPC(name, writer);
            }

            public BroadcastRpcPacket BroadcastRPC<T1, T2, T3, T4, T5>(VoidRpcMethod<T1, T2, T3, T4, T5> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);

                return BroadcastRPC(name, writer);
            }

            public BroadcastRpcPacket BroadcastRPC<T1, T2, T3, T4, T5, T6>(VoidRpcMethod<T1, T2, T3, T4, T5, T6> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);
                writer.Write(arg6);

                return BroadcastRPC(name, writer);
            }
            #endregion

            #region Return
            public BroadcastRpcPacket BroadcastRPC<TResult>(ReturnRpcMethod<TResult> rpc)
            {
                var name = RpcBind.GetName(rpc.Method);

                return BroadcastRPC(name, default);
            }

            public BroadcastRpcPacket BroadcastRPC<TResult, T1>(ReturnRpcMethod<TResult, T1> rpc, T1 arg1)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);

                return BroadcastRPC(name, writer);
            }

            public BroadcastRpcPacket BroadcastRPC<TResult, T1, T2>(ReturnRpcMethod<TResult, T1, T2> rpc, T1 arg1, T2 arg2)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);

                return BroadcastRPC(name, writer);
            }

            public BroadcastRpcPacket BroadcastRPC<TResult, T1, T2, T3>(ReturnRpcMethod<TResult, T1, T2, T3> rpc, T1 arg1, T2 arg2, T3 arg3)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);

                return BroadcastRPC(name, writer);
            }

            public BroadcastRpcPacket BroadcastRPC<TResult, T1, T2, T3, T4>(ReturnRpcMethod<TResult, T1, T2, T3, T4> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);

                return BroadcastRPC(name, writer);
            }

            public BroadcastRpcPacket BroadcastRPC<TResult, T1, T2, T3, T4, T5>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);

                return BroadcastRPC(name, writer);
            }

            public BroadcastRpcPacket BroadcastRPC<TResult, T1, T2, T3, T4, T5, T6>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);
                writer.Write(arg6);

                return BroadcastRPC(name, writer);
            }
            #endregion

            #endregion

            #region Target

            #region Void
            public TargetRpcPacket TargetRPC(VoidRpcMethod rpc, NetworkClient target)
            {
                var name = RpcBind.GetName(rpc.Method);

                return TargetRPC(name, target, default);
            }

            public TargetRpcPacket TargetRPC<T1>(VoidRpcMethod<T1> rpc, NetworkClient target, T1 arg1)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);

                return TargetRPC(name, target, writer);
            }

            public TargetRpcPacket TargetRPC<T1, T2>(VoidRpcMethod<T1, T2> rpc, NetworkClient target, T1 arg1, T2 arg2)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);

                return TargetRPC(name, target, writer);
            }

            public TargetRpcPacket TargetRPC<T1, T2, T3>(VoidRpcMethod<T1, T2, T3> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);

                return TargetRPC(name, target, writer);
            }

            public TargetRpcPacket TargetRPC<T1, T2, T3, T4>(VoidRpcMethod<T1, T2, T3, T4> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);

                return TargetRPC(name, target, writer);
            }

            public TargetRpcPacket TargetRPC<T1, T2, T3, T4, T5>(VoidRpcMethod<T1, T2, T3, T4, T5> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);

                return TargetRPC(name, target, writer);
            }

            public TargetRpcPacket TargetRPC<T1, T2, T3, T4, T5, T6>(VoidRpcMethod<T1, T2, T3, T4, T5, T6> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);
                writer.Write(arg6);

                return TargetRPC(name, target, writer);
            }
            #endregion

            #region Return
            public TargetRpcPacket TargetRPC<TResult>(ReturnRpcMethod<TResult> rpc, NetworkClient target)
            {
                var name = RpcBind.GetName(rpc.Method);

                return TargetRPC(name, target, default);
            }

            public TargetRpcPacket TargetRPC<TResult, T1>(ReturnRpcMethod<TResult, T1> rpc, NetworkClient target, T1 arg1)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);

                return TargetRPC(name, target, writer);
            }

            public TargetRpcPacket TargetRPC<TResult, T1, T2>(ReturnRpcMethod<TResult, T1, T2> rpc, NetworkClient target, T1 arg1, T2 arg2)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);

                return TargetRPC(name, target, writer);
            }

            public TargetRpcPacket TargetRPC<TResult, T1, T2, T3>(ReturnRpcMethod<TResult, T1, T2, T3> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);

                return TargetRPC(name, target, writer);
            }

            public TargetRpcPacket TargetRPC<TResult, T1, T2, T3, T4>(ReturnRpcMethod<TResult, T1, T2, T3, T4> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);

                return TargetRPC(name, target, writer);
            }

            public TargetRpcPacket TargetRPC<TResult, T1, T2, T3, T4, T5>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);

                return TargetRPC(name, target, writer);
            }

            public TargetRpcPacket TargetRPC<TResult, T1, T2, T3, T4, T5, T6>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);
                writer.Write(arg6);

                return TargetRPC(name, target, writer);
            }
            #endregion

            #endregion

            #region Query
            public QueryRpcPacket<TResult> QueryRPC<TResult>(ReturnRpcMethod<TResult> rpc, NetworkClient target)
            {
                var name = RpcBind.GetName(rpc.Method);

                return QueryRPC<TResult>(name, target, default);
            }

            public QueryRpcPacket<TResult> QueryRPC<TResult, T1>(ReturnRpcMethod<TResult, T1> rpc, NetworkClient target, T1 arg1)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);

                return QueryRPC<TResult>(name, target, writer);
            }

            public QueryRpcPacket<TResult> QueryRPC<TResult, T1, T2>(ReturnRpcMethod<TResult, T1, T2> rpc, NetworkClient target, T1 arg1, T2 arg2)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);

                return QueryRPC<TResult>(name, target, writer);
            }

            public QueryRpcPacket<TResult> QueryRPC<TResult, T1, T2, T3>(ReturnRpcMethod<TResult, T1, T2, T3> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);

                return QueryRPC<TResult>(name, target, writer);
            }

            public QueryRpcPacket<TResult> QueryRPC<TResult, T1, T2, T3, T4>(ReturnRpcMethod<TResult, T1, T2, T3, T4> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);

                return QueryRPC<TResult>(name, target, writer);
            }

            public QueryRpcPacket<TResult> QueryRPC<TResult, T1, T2, T3, T4, T5>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);

                return QueryRPC<TResult>(name, target, writer);
            }

            public QueryRpcPacket<TResult> QueryRPC<TResult, T1, T2, T3, T4, T5, T6>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6> rpc, NetworkClient target, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);
                writer.Write(arg6);

                return QueryRPC<TResult>(name, target, writer);
            }
            #endregion

            #region Buffer

            #region Void
            public BufferRpcPacket BufferRPC(VoidRpcMethod rpc)
            {
                var name = RpcBind.GetName(rpc.Method);

                return BufferRPC(name, default);
            }

            public BufferRpcPacket BufferRPC<T1>(VoidRpcMethod<T1> rpc, T1 arg1)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);

                return BufferRPC(name, writer);
            }

            public BufferRpcPacket BufferRPC<T1, T2>(VoidRpcMethod<T1, T2> rpc, T1 arg1, T2 arg2)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);

                return BufferRPC(name, writer);
            }

            public BufferRpcPacket BufferRPC<T1, T2, T3>(VoidRpcMethod<T1, T2, T3> rpc, T1 arg1, T2 arg2, T3 arg3)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);

                return BufferRPC(name, writer);
            }

            public BufferRpcPacket BufferRPC<T1, T2, T3, T4>(VoidRpcMethod<T1, T2, T3, T4> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);

                return BufferRPC(name, writer);
            }

            public BufferRpcPacket BufferRPC<T1, T2, T3, T4, T5>(VoidRpcMethod<T1, T2, T3, T4, T5> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);

                return BufferRPC(name, writer);
            }

            public BufferRpcPacket BufferRPC<T1, T2, T3, T4, T5, T6>(VoidRpcMethod<T1, T2, T3, T4, T5, T6> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);
                writer.Write(arg6);

                return BufferRPC(name, writer);
            }
            #endregion

            #region Return
            public BufferRpcPacket BufferRPC<TResult>(ReturnRpcMethod<TResult> rpc)
            {
                var name = RpcBind.GetName(rpc.Method);

                return BufferRPC(name, default);
            }

            public BufferRpcPacket BufferRPC<TResult, T1>(ReturnRpcMethod<TResult, T1> rpc, T1 arg1)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);

                return BufferRPC(name, writer);
            }

            public BufferRpcPacket BufferRPC<TResult, T1, T2>(ReturnRpcMethod<TResult, T1, T2> rpc, T1 arg1, T2 arg2)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);

                return BufferRPC(name, writer);
            }

            public BufferRpcPacket BufferRPC<TResult, T1, T2, T3>(ReturnRpcMethod<TResult, T1, T2, T3> rpc, T1 arg1, T2 arg2, T3 arg3)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);

                return BufferRPC(name, writer);
            }

            public BufferRpcPacket BufferRPC<TResult, T1, T2, T3, T4>(ReturnRpcMethod<TResult, T1, T2, T3, T4> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);

                return BufferRPC(name, writer);
            }

            public BufferRpcPacket BufferRPC<TResult, T1, T2, T3, T4, T5>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);

                return BufferRPC(name, writer);
            }

            public BufferRpcPacket BufferRPC<TResult, T1, T2, T3, T4, T5, T6>(ReturnRpcMethod<TResult, T1, T2, T3, T4, T5, T6> rpc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            {
                var name = RpcBind.GetName(rpc.Method);

                var writer = NetworkWriter.Pool.Take();
                writer.Write(arg1);
                writer.Write(arg2);
                writer.Write(arg3);
                writer.Write(arg4);
                writer.Write(arg5);
                writer.Write(arg6);

                return BufferRPC(name, writer);
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