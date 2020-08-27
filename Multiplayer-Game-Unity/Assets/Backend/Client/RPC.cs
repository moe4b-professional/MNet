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

        public string ID { get; protected set; }

        public MethodInfo MethodInfo { get; protected set; }

        public ParameterInfo[] ParametersInfo { get; protected set; }

        public NetworkRPCAttribute Attribute { get; protected set; }

        public RpcBufferMode BufferMode => Attribute.BufferMode;

        public static BindingFlags BindingFlags
        {
            get
            {
                return BindingFlags.Instance | BindingFlags.NonPublic;
            }
        }
        
        public RpcRequest CreateRequest(params object[] parameters)
        {
            var request = RpcRequest.Write(Entity.ID, Behaviour.ID, ID, BufferMode, parameters);

            return request;
        }

        public void InvokeCommand(RpcCommand command)
        {
            var parameters = command.Read(ParametersInfo);

            NetworkAPI.Room.Clients.TryGetValue(command.Sender, out var sender);
            var info = new RpcInfo(sender);
            parameters[parameters.Length - 1] = info;

            Invoke(parameters);
        }
        public void Invoke(params object[] parameters)
        {
            MethodInfo.Invoke(Behaviour, parameters);
        }

        public RpcBind(NetworkBehaviour behaviour, NetworkRPCAttribute attribute, MethodInfo method)
        {
            Behaviour = behaviour;

            Attribute = attribute;

            MethodInfo = method;
            ParametersInfo = method.GetParameters();
            ID = MethodInfo.Name;
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

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class NetworkRPCAttribute : Attribute
    {
        public RpcBufferMode BufferMode { get; private set; }

        public NetworkRPCAttribute() : this(RpcBufferMode.None) { }
        public NetworkRPCAttribute(RpcBufferMode bufferMode)
        {
            this.BufferMode = bufferMode;
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

    public partial class NetworkBehaviour
    {
        public void RequestRPC(RpcMethod callback)
            => RequestRPC(callback.Method);
        public void RequestRPC<T1>(RpcMethod<T1> callback, T1 arg1)
            => RequestRPC(callback.Method, arg1);
        public void RequestRPC<T1, T2>(RpcMethod<T1, T2> callback, T1 arg1, T2 arg2)
            => RequestRPC(callback.Method, arg1, arg2);
        public void RequestRPC<T1, T2, T3>(RpcMethod<T1, T2, T3> callback, T1 arg1, T2 arg2, T3 arg3)
            => RequestRPC(callback.Method, arg1, arg2, arg3);
        public void RequestRPC<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            => RequestRPC(callback.Method, arg1, arg2, arg3, arg4);
        public void RequestRPC<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
            => RequestRPC(callback.Method, arg1, arg2, arg3, arg4, arg5);
        public void RequestRPC<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> callback, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
            => RequestRPC(callback.Method, arg1, arg2, arg3, arg4, arg5, arg6);
    }
}