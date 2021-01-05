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
    #region Call
    public class RpcBind
    {
        public NetworkBehaviour Behaviour { get; protected set; }
        public NetworkEntity Entity => Behaviour.Entity;

        public NetworkRPCAttribute Attribute { get; protected set; }
        public RemoteAuthority Authority => Attribute.Authority;
        public DeliveryMode DeliveryMode => Attribute.Delivery;

        public MethodInfo MethodInfo { get; protected set; }
        public RpxMethodID MethodID { get; protected set; }

        public string Name { get; protected set; }

        public ParameterInfo[] ParametersInfo { get; protected set; }

        public Type ReturnType => MethodInfo.ReturnType;

        public bool HasInfoParameter { get; protected set; }

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

        public RpcBind(NetworkBehaviour behaviour, NetworkRPCAttribute attribute, MethodInfo method, byte index)
        {
            Behaviour = behaviour;

            Attribute = attribute;

            MethodInfo = method;
            Name = GetName(MethodInfo);
            MethodID = new RpxMethodID(index);

            ParametersInfo = method.GetParameters();

            HasInfoParameter = ParametersInfo?.LastOrDefault()?.ParameterType == typeof(RpcInfo);
        }

        public static string GetName(MethodInfo method) => method.Name;
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

            this.IsBuffered = NetworkAPI.Realtime.IsOnBuffer;
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class NetworkRPCAttribute : Attribute
    {
        public RemoteAuthority Authority { get; set; } = RemoteAuthority.Any;
        public DeliveryMode Delivery { get; set; } = DeliveryMode.Reliable;

        public NetworkRPCAttribute() { }

        public static NetworkRPCAttribute Retrieve(MethodInfo info) => info.GetCustomAttribute<NetworkRPCAttribute>(true);

        public static bool Defined(MethodInfo info) => Retrieve(info) != null;
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
    public class RprPromise
    {
        public NetworkClient Target { get; protected set; }
        public RprChannelID Channel { get; protected set; }

        public bool Complete { get; protected set; }
        public bool IsComplete() => Complete;

        public RemoteResponseType Response { get; protected set; }

        public byte[] Raw { get; protected set; }
        internal T Read<T>() => Response == RemoteResponseType.Success ? NetworkSerializer.Deserialize<T>(Raw) : default;

        internal void Fullfil(RprCommand command)
        {
            Complete = true;

            Response = command.Response;
            Raw = command.Raw;
        }

        public RprPromise(NetworkClient target, RprChannelID channel)
        {
            this.Target = target;
            this.Channel = channel;

            Complete = false;
        }
    }

    public struct RprAnswer<T>
    {
        public RemoteResponseType Response { get; private set; }
        public bool Success => Response == RemoteResponseType.Success;

        public T Value { get; private set; }

        internal RprAnswer(RemoteResponseType response)
        {
            this.Response = response;
            Value = default;
        }

        internal RprAnswer(RprPromise promise)
        {
            Response = promise.Response;
            Value = promise.Read<T>();
        }
    }

    public delegate void RprCallback<T>(RemoteResponseType response, T value);

    public delegate TResult RpcQueryMethod<TResult>(RpcInfo info);
    public delegate TResult RpcQueryMethod<TResult, T1>(T1 arg1, RpcInfo info);
    public delegate TResult RpcQueryMethod<TResult, T1, T2>(T1 arg1, T2 arg2, RpcInfo info);
    public delegate TResult RpcQueryMethod<TResult, T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, RpcInfo info);
    public delegate TResult RpcQueryMethod<TResult, T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, RpcInfo info);
    public delegate TResult RpcQueryMethod<TResult, T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RpcInfo info);
    public delegate TResult RpcQueryMethod<TResult, T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RpcInfo info);
    #endregion
}