using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;

namespace Backend
{
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

        public static BindingFlags BindingFlags
        {
            get
            {
                return BindingFlags.Instance | BindingFlags.NonPublic;
            }
        }

        public BroadcastRpcRequest CreateRequest(RpcBufferMode bufferMode, params object[] arguments)
        {
            var request = BroadcastRpcRequest.Write(Entity.ID, Behaviour.ID, ID, bufferMode, arguments);

            return request;
        }
        public TargetRpcRequest CreateRequest(NetworkClientID client, params object[] arguments)
        {
            var request = TargetRpcRequest.Write(client, Entity.ID, Behaviour.ID, ID, arguments);

            return request;
        }

        public void Invoke(RpcCommand command)
        {
            var arguments = command.Read(ParametersInfo, HasInfoParameter ? 1 : 0);

            if(HasInfoParameter)
            {
                NetworkAPI.Room.Clients.TryGetValue(command.Sender, out var sender);

                var info = new RpcInfo(sender);

                arguments[arguments.Length - 1] = info;
            }

            Invoke(arguments);
        }
        public void Invoke(params object[] arguments)
        {
            MethodInfo.Invoke(Behaviour, arguments);
        }

        public RpcBind(NetworkBehaviour behaviour, NetworkRPCAttribute attribute, MethodInfo method)
        {
            Behaviour = behaviour;

            Attribute = attribute;

            MethodInfo = method;
            ParametersInfo = method.GetParameters();
            ID = MethodInfo.Name;

            HasInfoParameter = ParametersInfo?.LastOrDefault()?.ParameterType == typeof(RpcInfo);
        }
    }

    public struct RpcInfo
    {
        public NetworkClient Sender { get; private set; }

        public RpcInfo(NetworkClient sender)
        {
            this.Sender = sender;
        }
    }

    public enum RpcAuthority
    {
        Any,
        Owner,
        Master,
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

    public class RpcCollection
    {
        public Dictionary<string, RpcBind> Dictionary { get; protected set; }

        public BindingFlags BindingFlags => BindingFlags.Instance | BindingFlags.NonPublic;

        public bool Find(string name, out RpcBind bind)
        {
            return Dictionary.TryGetValue(name, out bind);
        }

        public RpcCollection(NetworkBehaviour behaviour)
        {
            Dictionary = new Dictionary<string, RpcBind>();

            foreach (var method in behaviour.GetType().GetMethods(BindingFlags))
            {
                var attribute = method.GetCustomAttribute<NetworkRPCAttribute>();

                if (attribute == null) continue;

                var bind = new RpcBind(behaviour, attribute, method);

                if (Dictionary.ContainsKey(bind.ID))
                    throw new Exception($"Rpc Named {bind.ID} Already Registered On Behaviour {behaviour.GetType()}, Please Assign Every RPC a Unique Name And Don't Overload the RPC Methods");

                Dictionary.Add(bind.ID, bind);
            }
        }
    }

    public delegate void RpcMethod(RpcInfo info);
    public delegate void RpcMethod<T1>(T1 arg1, RpcInfo info);
    public delegate void RpcMethod<T1, T2>(T1 arg1, T2 arg2, RpcInfo info);
    public delegate void RpcMethod<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, RpcInfo info);
    public delegate void RpcMethod<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, RpcInfo info);
    public delegate void RpcMethod<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RpcInfo info);
    public delegate void RpcMethod<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RpcInfo info);
}