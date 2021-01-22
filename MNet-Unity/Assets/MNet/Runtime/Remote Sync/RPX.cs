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

using Cysharp.Threading.Tasks;

namespace MNet
{
    #region Call
    public class RpcBind
    {
        public string Name { get; protected set; }

        public NetworkBehaviour Behaviour { get; protected set; }
        public NetworkEntity Entity => Behaviour.Entity;

        #region Attribute
        public NetworkRPCAttribute Attribute { get; protected set; }

        public RemoteAuthority Authority => Attribute.Authority;
        #endregion

        #region Method
        public MethodInfo MethodInfo { get; protected set; }

        public RpxMethodID MethodID { get; protected set; }

        public ParameterInfo[] ParametersInfo { get; protected set; }

        public Type ReturnType => MethodInfo.ReturnType;

        public bool IsAsync { get; protected set; }
        public bool IsCoroutine { get; protected set; }
        #endregion

        public object Invoke(params object[] arguments)
        {
            return MethodInfo.Invoke(Behaviour, arguments);
        }

        public byte[] WriteArguments(object[] arguments)
        {
            var writer = NetworkWriter.Pool.Any;

            for (int i = 0; i < arguments.Length; i++)
                writer.Write(arguments[i], ParametersInfo[i].ParameterType);

            var raw = writer.ToArray();

            NetworkWriter.Pool.Return(writer);

            return raw;
        }

        public object[] ReadArguments(RpcCommand command, out RpcInfo info)
        {
            var arguments = command.Read(ParametersInfo, 1);

            NetworkAPI.Room.Clients.TryGet(command.Sender, out var sender);

            info = new RpcInfo(sender);

            arguments[arguments.Length - 1] = info;

            return arguments;
        }

        public override string ToString() => $"{Behaviour}->{Name}";

        public RpcBind(NetworkBehaviour behaviour, NetworkRPCAttribute attribute, MethodInfo method, byte index)
        {
            this.Behaviour = behaviour;
            this.Attribute = attribute;

            MethodInfo = method;
            MethodID = new RpxMethodID(index);

            ParametersInfo = method.GetParameters();

            if (ParametersInfo.LastOrDefault()?.ParameterType != typeof(RpcInfo))
                throw new Exception($"Cannot use '{Behaviour}->{MethodInfo.Name}' as RPC, the Last Parameter needs to be of Type {nameof(RpcInfo)} for it to be a Valid RPC");

            Name = GetName(MethodInfo);

            IsAsync = typeof(IUniTask).IsAssignableFrom(ReturnType);
            IsCoroutine = ReturnType == typeof(IEnumerator);
        }

        public static string GetName(MethodInfo method) => method.Name;

        public static int Count(params bool[] list)
        {
            var count = 0;

            for (int i = 0; i < list.Length; i++)
                if (list[i])
                    count += 1;

            return count;
        }
    }

    public struct RpcInfo
    {
        public NetworkClient Sender { get; private set; }

        /// <summary>
        /// Is this RPC Request the Result of the Room's Buffer
        /// </summary>
        public bool IsBuffered { get; private set; }

        public RpcInfo(NetworkClient sender)
        {
            this.Sender = sender;

            this.IsBuffered = NetworkAPI.Realtime.IsOnBuffer;
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class NetworkRPCAttribute : Attribute
    {
        public RemoteAuthority Authority { get; set; } = RemoteAuthority.Any;

        public NetworkRPCAttribute() { }

        public static NetworkRPCAttribute Retrieve(MethodInfo info) => info.GetCustomAttribute<NetworkRPCAttribute>(true);

        public static bool Defined(MethodInfo info) => Retrieve(info) != null;
    }
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

        internal void Fullfil(RemoteResponseType response, byte[] raw)
        {
            Complete = true;

            this.Response = response;
            this.Raw = raw;
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
        public bool Fail => Response != RemoteResponseType.Success;

        public T Value { get; private set; }

        internal RprAnswer(RemoteResponseType response, T value)
        {
            this.Response = response;
            this.Value = value;
        }
        internal RprAnswer(RemoteResponseType response) : this(response, default) { }
        internal RprAnswer(RprPromise promise) : this(promise.Response, promise.Read<T>()) { }
    }
    #endregion
}