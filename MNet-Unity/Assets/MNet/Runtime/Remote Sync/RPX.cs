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

        public Type ParentType { get; protected set; }

        public NetworkBehaviour Behaviour { get; protected set; }
        public NetworkEntity Entity => Behaviour.Entity;

        #region Attribute
        public NetworkRPCAttribute Attribute { get; protected set; }
        public RemoteAuthority Authority => Attribute.Authority;
        public DeliveryMode DeliveryMode => Attribute.Delivery;
        #endregion

        #region Method
        public MethodInfo MethodInfo { get; protected set; }

        public RpxMethodID MethodID { get; protected set; }

        public ParameterInfo[] ParametersInfo { get; protected set; }
        public bool HasInfoParameter { get; protected set; }

        public Type ReturnType => MethodInfo.ReturnType;

        public bool IsAsync { get; protected set; }
        public bool IsCoroutine { get; protected set; }
        #endregion

        public object Invoke(params object[] arguments)
        {
            return MethodInfo.Invoke(Behaviour, arguments);
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

        public override string ToString() => $"{ParentType}->{Name}";

        public RpcBind(NetworkBehaviour behaviour, NetworkRPCAttribute attribute, MethodInfo method, byte index)
        {
            Behaviour = behaviour;

            ParentType = behaviour.GetType();

            Attribute = attribute;

            MethodInfo = method;
            MethodID = new RpxMethodID(index);

            ParametersInfo = method.GetParameters();
            HasInfoParameter = ParametersInfo?.LastOrDefault()?.ParameterType == typeof(RpcInfo);

            Name = GetName(MethodInfo);

            IsAsync = typeof(IUniTask).IsAssignableFrom(ReturnType);
            IsCoroutine = ReturnType == typeof(IEnumerator);
        }

        public static string GetName(MethodInfo method) => method.Name;
    }

    public struct RpcInfo
    {
        public NetworkClient Sender { get; private set; }

        /// <summary>
        /// Time this RPC Request was Invoked on the Server
        /// </summary>
        public NetworkTimeSpan Time { get; private set; }

        /// <summary>
        /// Is this RPC Request the Result of the Room's Buffer
        /// </summary>
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