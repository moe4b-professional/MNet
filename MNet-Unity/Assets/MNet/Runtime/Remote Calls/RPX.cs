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

namespace MNet
{
    [Flags]
    public enum RemoteAuthority : byte
    {
        /// <summary>
        /// As the name implies, any client will be able to the remote action
        /// </summary>
        Any = 1 << 0,

        /// <summary>
        /// Only the owner of this entity may invoke this this remote action
        /// </summary>
        Owner = 1 << 1,

        /// <summary>
        /// Only the master client may invoke this remote action
        /// </summary>
        Master = 1 << 2,
    }

    #region Call
    public class RpcBind
    {
        public NetworkBehaviour Behaviour { get; protected set; }
        public NetworkEntity Entity => Behaviour.Entity;

        public NetworkRPCAttribute Attribute { get; protected set; }
        public RemoteAuthority Authority => Attribute.Authority;
        public DeliveryMode DeliveryMode => Attribute.Delivery;

        public MethodInfo MethodInfo { get; protected set; }

        public string Name { get; protected set; }

        public ParameterInfo[] ParametersInfo { get; protected set; }

        public Type ReturnType => MethodInfo.ReturnType;

        public bool HasInfoParameter { get; protected set; }

        public RpcRequest CreateRequest(RpcBufferMode bufferMode, params object[] arguments)
        {
            var request = RpcRequest.Write(Entity.ID, Behaviour.ID, Name, bufferMode, arguments);

            return request;
        }
        public RpcRequest CreateRequest(NetworkClientID target, params object[] arguments)
        {
            var request = RpcRequest.Write(Entity.ID, Behaviour.ID, Name, target, arguments);

            return request;
        }
        public RpcRequest CreateRequest(NetworkClientID target, ushort callback, params object[] arguments)
        {
            var request = RpcRequest.Write(Entity.ID, Behaviour.ID, Name, target, callback, arguments);

            return request;
        }

        public object[] ParseArguments(RpcCommand command)
        {
            var arguments = command.Read(ParametersInfo, HasInfoParameter ? 1 : 0);

            NetworkAPI.Room.Clients.TryGetValue(command.Sender, out var sender);

            if (HasInfoParameter)
            {
                var info = new RpcInfo(sender, command.Time);

                arguments[arguments.Length - 1] = info;
            }

            return arguments;
        }

        public object Invoke(params object[] arguments)
        {
            return MethodInfo.Invoke(Behaviour, arguments);
        }

        public override string ToString() => $"{Entity}->{Name}";

        public RpcBind(NetworkBehaviour behaviour, NetworkRPCAttribute attribute, MethodInfo method)
        {
            Behaviour = behaviour;

            Attribute = attribute;

            MethodInfo = method;
            Name = MethodInfo.Name;

            ParametersInfo = method.GetParameters();

            HasInfoParameter = ParametersInfo?.LastOrDefault()?.ParameterType == typeof(RpcInfo);
        }
    }

    public struct RpcInfo
    {
        public NetworkClient Sender { get; private set; }

        public NetworkTimeSpan Time { get; private set; }

        public bool IsBuffered { get; private set; }

        public RpcInfo(NetworkClient sender, NetworkTimeSpan time)
        {
            this.Sender = sender;
            this.Time = time;

            this.IsBuffered = NetworkAPI.Room.IsApplyingMessageBuffer;
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class NetworkRPCAttribute : Attribute
    {
        public RemoteAuthority Authority { get; set; } = RemoteAuthority.Any;
        public DeliveryMode Delivery { get; set; } = DeliveryMode.Reliable;

        public NetworkRPCAttribute() { }
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

            arguments[0] = command.Result;

            if (command.Result == RprResult.Success)
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

        public static object GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

        public static ushort Increment(ushort id) => id += 1;
    }

    public delegate void RprCallback<T>(RprResult result, T value);

    public delegate TResult RprMethod<TResult>(RpcInfo info);
    public delegate TResult RprMethod<TResult, T1>(T1 arg1, RpcInfo info);
    public delegate TResult RprMethod<TResult, T1, T2>(T1 arg1, T2 arg2, RpcInfo info);
    public delegate TResult RprMethod<TResult, T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, RpcInfo info);
    public delegate TResult RprMethod<TResult, T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, RpcInfo info);
    public delegate TResult RprMethod<TResult, T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RpcInfo info);
    public delegate TResult RprMethod<TResult, T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RpcInfo info);
    #endregion
}