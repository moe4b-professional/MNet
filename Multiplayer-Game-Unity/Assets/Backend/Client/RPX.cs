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
    #region Call
    public class RpcBind
    {
        public NetworkEntity Entity => Behaviour.Entity;

        public NetworkBehaviour Behaviour { get; protected set; }

        public NetworkRPCAttribute Attribute { get; protected set; }

        public RpcAuthority Authority => Attribute.Authority;

        public MethodInfo MethodInfo { get; protected set; }

        public string ID { get; protected set; }

        public ParameterInfo[] ParametersInfo { get; protected set; }
        public bool HasInfoParameter { get; protected set; }

        public bool HasReturn => MethodInfo.ReturnType != null;

        public RpcRequest CreateRequest(RpcBufferMode bufferMode, params object[] arguments)
        {
            var request = RpcRequest.Write(Entity.ID, Behaviour.ID, ID, bufferMode, arguments);

            return request;
        }
        public RpcRequest CreateRequest(NetworkClientID target, params object[] arguments)
        {
            var request = RpcRequest.Write(Entity.ID, Behaviour.ID, ID, target, arguments);

            return request;
        }
        public RpcRequest CreateRequest(NetworkClientID target, ushort callback, params object[] arguments)
        {
            var request = RpcRequest.Write(Entity.ID, Behaviour.ID, ID, target, callback, arguments);

            return request;
        }

        public object[] ParseArguments(RpcCommand command, out RpcInfo info)
        {
            var arguments = command.Read(ParametersInfo, HasInfoParameter ? 1 : 0);

            NetworkAPI.Room.Clients.TryGetValue(command.Sender, out var sender);

            info = new RpcInfo(sender);

            if (HasInfoParameter) arguments[arguments.Length - 1] = info;

            return arguments;
        }

        public object Invoke(params object[] arguments)
        {
            return MethodInfo.Invoke(Behaviour, arguments);
        }

        public RpcBind(NetworkBehaviour behaviour, NetworkRPCAttribute attribute, MethodInfo method)
        {
            Behaviour = behaviour;

            Attribute = attribute;

            MethodInfo = method;
            ID = MethodInfo.Name;

            ParametersInfo = method.GetParameters();
            HasInfoParameter = ParametersInfo?.LastOrDefault()?.ParameterType == typeof(RpcInfo);
        }
    }

    public enum RpcAuthority : byte
    {
        /// <summary>
        /// As the name implies, any client will be able to invoke this RPC
        /// </summary>
        Any,

        /// <summary>
        /// Only the owner of this entity may invoke this RPC
        /// </summary>
        Owner,

        /// <summary>
        /// Only the master client may invoke this RPC
        /// </summary>
        Master,
    }

    public struct RpcInfo
    {
        public NetworkClient Sender { get; private set; }

        public RpcInfo(NetworkClient sender)
        {
            this.Sender = sender;
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class NetworkRPCAttribute : Attribute
    {
        public RpcAuthority Authority { get; private set; }

        public NetworkRPCAttribute(RpcAuthority authority = RpcAuthority.Any)
        {
            this.Authority = authority;
        }
    }

    public delegate void RpcMethod(RpcInfo info);
    public delegate void RpcMethod<T1>(T1 arg1, RpcInfo info);
    public delegate void RpcMethod<T1, T2>(T1 arg1, T2 arg2, RpcInfo info);
    public delegate void RpcMethod<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, RpcInfo info);
    public delegate void RpcMethod<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, RpcInfo info);
    public delegate void RpcMethod<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RpcInfo info);
    public delegate void RpcMethod<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RpcInfo info);
    #endregion

    #region Return
    public class RprBind
    {
        public ushort ID { get; protected set; }

        public object Target { get; protected set; }

        public MethodInfo Method { get; protected set; }
        public ParameterInfo[] Parameters { get; protected set; }

        public Type ReturnType { get; protected set; }

        public object[] ParseArguments(RprCommand command)
        {
            var arguments = new object[2];

            arguments[0] = command.Success;

            if (command.Success)
                arguments[1] = command.Read(ReturnType);
            else
                arguments[1] = GetDefault(ReturnType);

            return arguments;
        }

        public void Invoke(params object[] arguments) => Method.Invoke(Target, arguments);

        public RprBind(ushort id, MethodInfo method, object target)
        {
            this.ID = id;
            this.Target = target;
            this.Method = method;

            Parameters = method.GetParameters();

            this.ReturnType = Parameters[1].ParameterType;
        }

        public static object GetDefault(Type type)
        {
            if (type.IsValueType) return Activator.CreateInstance(type);

            return null;
        }
    }

    public delegate void RprMethod<T>(bool success, T result);

    public delegate TResult RpcReturnMethod<TResult>(RpcInfo info);
    public delegate TResult RpcReturnMethod<TResult, T1>(T1 arg1, RpcInfo info);
    public delegate TResult RpcReturnMethod<TResult, T1, T2>(T1 arg1, T2 arg2, RpcInfo info);
    public delegate TResult RpcReturnMethod<TResult, T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, RpcInfo info);
    public delegate TResult RpcReturnMethod<TResult, T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, RpcInfo info);
    public delegate TResult RpcReturnMethod<TResult, T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RpcInfo info);
    public delegate TResult RpcReturnMethod<TResult, T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RpcInfo info);
    #endregion
}