using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;

namespace Game.Shared
{
    public class RPCBind
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

        public void Invoke(RPCPayload payload)
        {
            var parameters = payload.Read(ParametersInfo);

            Invoke(parameters);
        }
        public void Invoke(params object[] arameters)
        {
            MethodInfo.Invoke(Owner, arameters);
        }

        public RPCBind(object owner, MethodInfo method)
        {
            Owner = owner;

            MethodInfo = method;

            ParametersInfo = method.GetParameters();
        }

        public static RPCBind Parse(object owner, string name)
        {
            var method = owner.GetType().GetMethod(name, BindingFlags);

            var bind = new RPCBind(owner, method);

            return bind;
        }
    }
}