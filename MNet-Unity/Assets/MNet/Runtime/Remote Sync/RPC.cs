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
    public class RpcBind
    {
        public RpcID ID { get; protected set; }
        public string Name { get; protected set; }

        public NetworkEntity.Behaviour Behaviour { get; protected set; }
        public Component Component => Behaviour.Component;
        public NetworkEntity Entity => Behaviour.Entity;

        #region Attribute
        public NetworkRPCAttribute Attribute { get; protected set; }

        public RemoteAuthority Authority => Attribute.Authority;
        #endregion

        #region Method
        public MethodInfo MethodInfo { get; protected set; }

        public ParameterInfo[] ParametersInfo { get; protected set; }

        public Type ReturnType => MethodInfo.ReturnType;

        public bool IsBinaryExchange { get; protected set; }

        public bool IsAsync { get; protected set; }
        public bool IsCoroutine { get; protected set; }
        #endregion

        public object Invoke(params object[] arguments)
        {
            return MethodInfo.Invoke(Component, arguments);
        }

        public byte[] SerializeArguments(object[] arguments)
        {
            if (IsBinaryExchange)
            {
                if (arguments[0] is byte[] buffer)
                    return buffer;
                else
                    throw new Exception($"RPC {this} Marked as Binary Exchange but the Only Parameter isn't a byte[]");
            }

            using (NetworkStream.Pool.Writer.Lease(out var stream))
            {
                for (int i = 0; i < arguments.Length; i++)
                    stream.Write(arguments[i], ParametersInfo[i].ParameterType);

                return stream.ToArray();
            }
        }

        public void ParseCommand<T>(T command, out object[] arguments, out RpcInfo info)
            where T : IRpcCommand
        {
            using (NetworkStream.Pool.Reader.Lease(out var stream))
            {
                stream.Assign(command.Raw);

                arguments = new object[ParametersInfo.Length];

                if (IsBinaryExchange)
                {
                    arguments[0] = command.Raw;
                }
                else
                {
                    for (int i = 0; i < ParametersInfo.Length - 1; i++)
                        arguments[i] = stream.Read(ParametersInfo[i].ParameterType);
                }
            }

            NetworkAPI.Room.Clients.TryGet(command.Sender, out var sender);
            info = new RpcInfo(sender);

            arguments[arguments.Length - 1] = info;
        }

        public override string ToString() => $"{Behaviour}->{Name}";

        public RpcBind(NetworkEntity.Behaviour behaviour, NetworkRPCAttribute attribute, MethodInfo method, byte index)
        {
            this.Behaviour = behaviour;
            this.Attribute = attribute;

            MethodInfo = method;
            ID = new RpcID(index);

            ParametersInfo = method.GetParameters();

            if (ParametersInfo.LastOrDefault()?.ParameterType != typeof(RpcInfo))
                throw new Exception($"Cannot use '{Behaviour}->{MethodInfo.Name}' as RPC, the Last Parameter needs to be of Type {nameof(RpcInfo)} for it to be a Valid RPC");

            Name = GetName(MethodInfo);

            IsBinaryExchange = ParametersInfo.Length == 2 && ParametersInfo[0].ParameterType == typeof(byte[]);

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

            this.IsBuffered = NetworkAPI.Client.Buffer.IsOn;
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
}