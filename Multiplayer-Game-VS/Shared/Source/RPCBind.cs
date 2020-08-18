using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;

namespace Game.Shared
{
    public class RpcBind
    {
        public object Owner { get; protected set; }
        
        public MethodInfo MethodInfo { get; protected set; }

        public ParameterInfo[] ParametersInfo { get; protected set; }

        public static BindingFlags BindingFlags
        {
            get
            {
                return BindingFlags.Instance | BindingFlags.NonPublic;
            }
        }

        public RpcPayload Request(RpcCallback callback) => Request();
        public RpcPayload Request<T1>(RpcCallback<T1> callback, T1 arg1) => Request(arg1);
        public RpcPayload Request<T1, T2>(RpcCallback<T1, T2> callback, T1 arg1, T2 arg2) => Request(arg1, arg2);
        public RpcPayload Request<T1, T2, T3>(RpcCallback<T1, T2, T3> callback, T1 arg1, T2 arg2, T3 arg3) => Request(arg1, arg2, arg3);

        public RpcPayload Request(params object[] parameters)
        {
            var payload = RpcPayload.Write("Target ID", parameters);

            return payload;
        }

        public void Invoke(RpcPayload payload)
        {
            var parameters = payload.Read(ParametersInfo);

            Invoke(parameters);
        }
        public void Invoke(params object[] parameters)
        {
            MethodInfo.Invoke(Owner, parameters);
        }

        public RpcBind(object owner, MethodInfo method)
        {
            Owner = owner;

            MethodInfo = method;

            ParametersInfo = method.GetParameters();
        }

        public static RpcBind Parse(object owner, string name)
        {
            var method = owner.GetType().GetMethod(name, BindingFlags);

            var bind = new RpcBind(owner, method);

            return bind;
        }
    }

    public delegate void RpcCallback();
    public delegate void RpcCallback<T1>(T1 arg1);
    public delegate void RpcCallback<T1, T2>(T1 arg1, T2 arg2);
    public delegate void RpcCallback<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);
}