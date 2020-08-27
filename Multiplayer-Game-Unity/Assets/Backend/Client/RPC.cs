using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;

using Backend.Shared;

namespace Backend.Client
{
    public class RpcBind
    {
        public NetworkEntity Entity => Behaviour.Entity;

        public NetworkBehaviour Behaviour { get; protected set; }

        public string Name { get; protected set; }

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
            var request = RpcRequest.Write(Entity.ID, Behaviour.ID, Name, BufferMode, parameters);

            return request;
        }

        public void Invoke(RpcCommand command)
        {
            var parameters = command.Read(ParametersInfo);

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
            Name = MethodInfo.Name;
            ParametersInfo = method.GetParameters();
        }
    }
    
    public delegate void RpcCallback();
    public delegate void RpcCallback<T1>(T1 arg1);
    public delegate void RpcCallback<T1, T2>(T1 arg1, T2 arg2);
    public delegate void RpcCallback<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);

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
}