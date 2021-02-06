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
        public string Name { get; protected set; }

        public NetworkBehaviour Behaviour { get; protected set; }
        public NetworkEntity Entity => Behaviour.Entity;

        #region Attribute
        public NetworkRPCAttribute Attribute { get; protected set; }

        public RemoteAuthority Authority => Attribute.Authority;
        #endregion

        #region Method
        public MethodInfo MethodInfo { get; protected set; }

        public RpcMethodID MethodID { get; protected set; }

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

        public void ParseCommand(IRpcCommand command, out object[] arguments, out RpcInfo info)
        {
            var reader = new NetworkReader(command.Raw);

            arguments = new object[ParametersInfo.Length];

            for (int i = 0; i < ParametersInfo.Length - 1; i++)
            {
                var value = reader.Read(ParametersInfo[i].ParameterType);

                arguments[i] = value;
            }

            NetworkAPI.Room.Clients.TryGet(command.Sender, out var sender);
            info = new RpcInfo(sender);

            arguments[arguments.Length - 1] = info;
        }

        public override string ToString() => $"{Behaviour}->{Name}";

        public RpcBind(NetworkBehaviour behaviour, NetworkRPCAttribute attribute, MethodInfo method, byte index)
        {
            this.Behaviour = behaviour;
            this.Attribute = attribute;

            MethodInfo = method;
            MethodID = new RpcMethodID(index);

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
}