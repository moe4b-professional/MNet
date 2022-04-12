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
using MB;

namespace MNet
{
    [Preserve]
    public abstract class RpcBind
    {
        public RpcID ID { get; protected set; }
        public string Name { get; protected set; }

        public NetworkEntity.Behaviour Behaviour { get; protected set; }
        public MonoBehaviour Component => Behaviour.Component;
        public NetworkEntity Entity => Behaviour.Entity;

        public abstract void Configure(NetworkEntity.Behaviour behaviour, NetworkRPCAttribute attribute, MethodInfo method, byte index);

        #region Attribute
        public NetworkRPCAttribute Attribute { get; protected set; }

        public RemoteAuthority Authority => Attribute.Authority;
        #endregion

        #region Method
        public Type ReturnType { get; protected set; }
        public bool HasReturn => ReturnType != typeof(void);
        #endregion

        public virtual RpcInfo ParseInfo<TCommand>(TCommand command)
            where TCommand : IRpcCommand
        {
            NetworkAPI.Room.Clients.TryGet(command.Sender, out var sender);
            return new RpcInfo(sender);
        }

        public abstract void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info);

        public override string ToString() => $"{Behaviour}->{Name}";

        //Static Utility

        public static string GetName(MethodInfo method) => method.Name;

        public static RpcBind Retrieve(MethodInfo method)
        {
            var parameters = method.GetParameters();

            if (parameters.Length < 1)
                throw new ArgumentException($"Method must Have at Least 1 Argument");

            if (parameters.Last().ParameterType != typeof(RpcInfo))
                throw new ArgumentException($"Last Parameter must be RPC Info");

            var index = parameters.Length - 1;

            if (method.ReturnType == typeof(void))
            {
                switch (index)
                {
                    case 0:
                        return Create(typeof(RpcBindVoid), method, parameters);

                    case 1:
                        return Create(typeof(RpcBindVoid<>), method, parameters);

                    case 2:
                        return Create(typeof(RpcBindVoid<,>), method, parameters);

                    case 3:
                        return Create(typeof(RpcBindVoid<,,>), method, parameters);

                    case 4:
                        return Create(typeof(RpcBindVoid<,,,>), method, parameters);

                    case 5:
                        return Create(typeof(RpcBindVoid<,,,,>), method, parameters);

                    case 6:
                        return Create(typeof(RpcBindVoid<,,,,,>), method, parameters);

                    default:
                        throw new ArgumentException("Too Many Arguments");
                }
            }
            else
            {
                switch (index)
                {
                    case 0:
                        return Create(typeof(RpcBindReturn<>), method, parameters);

                    case 1:
                        return Create(typeof(RpcBindReturn<,>), method, parameters);

                    case 2:
                        return Create(typeof(RpcBindReturn<,,>), method, parameters);

                    case 3:
                        return Create(typeof(RpcBindReturn<,,,>), method, parameters);

                    case 4:
                        return Create(typeof(RpcBindReturn<,,,,>), method, parameters);

                    case 5:
                        return Create(typeof(RpcBindReturn<,,,,,>), method, parameters);

                    case 6:
                        return Create(typeof(RpcBindReturn<,,,,,,>), method, parameters);

                    default:
                        throw new ArgumentException("Too Many Arguments");
                }
            }
        }

        protected static RpcBind Create(Type prototype, MethodInfo method, ParameterInfo[] parameters)
        {
            Type[] arguments;

            if (method.ReturnType == typeof(void))
            {
                arguments = TypeArrayCache.Retrieve(parameters.Length - 1);

                for (int i = 0; i < parameters.Length - 1; i++)
                    arguments[i] = parameters[i].ParameterType;
            }
            else
            {
                arguments = TypeArrayCache.Retrieve(parameters.Length - 1 + 1);

                arguments[0] = method.ReturnType;

                for (int i = 1; i < parameters.Length - 1; i++)
                    arguments[i] = parameters[i - 1].ParameterType;
            }

            var type = prototype.IsGenericType ? prototype.MakeGenericType(arguments) : prototype;

            var instance = Activator.CreateInstance(type) as RpcBind;

            return instance;
        }

        internal static class TypeArrayCache
        {
            static Type[][] Arrays;

            public static Type[] Retrieve(int count)
            {
                if (count == 0)
                    return null;

                return Arrays[count - 1];
            }

            static TypeArrayCache()
            {
                Arrays = new Type[7][];

                for (int i = 0; i < Arrays.Length; i++)
                    Arrays[i] = new Type[i + 1];
            }
        }

        public static class Parser
        {
            public const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            public static Dictionary<Type, Prototype[]> Dictionary { get; }
            
            public static Prototype[] Retrieve(Type type)
            {
                if (Dictionary.TryGetValue(type, out var array))
                    return array;

                array = Parse(type);
                Dictionary[type] = array;
                return array;
            }

            static List<Prototype> CacheList;
            static HashSet<string> CacheHashSet;
            static Comparison<Prototype> Comparer;

            static Prototype[] Parse(Type type)
            {
                {
                    CacheList.Clear();
                    CacheHashSet.Clear();

                    var array = type.GetMethods(Flags);

                    for (int i = 0; i < array.Length; i++)
                    {
                        var attribute = NetworkRPCAttribute.Retrieve(array[i]);
                        if (attribute is null)
                            continue;

                        var name = GetName(array[i]);

                        if (CacheHashSet.Add(name) == false)
                            throw new Exception($"Multiple '{name}' RPCs Registered On '{type}', Please Assign Every RPC a Unique Name And Don't Overload RPC Methods");

                        var data = new Prototype(array[i], attribute);
                        CacheList.Add(data);
                    }
                }

                CacheList.Sort(Comparer);

                if (CacheList.Count > byte.MaxValue)
                    throw new Exception($"NetworkBehaviour {type} Can't Have More than {byte.MaxValue} RPCs Defined");

                return CacheList.ToArray();
            }

            static Parser()
            {
                Dictionary = new Dictionary<Type, Prototype[]>();
                CacheList = new List<Prototype>();
                CacheHashSet = new HashSet<string>();

                Comparer = (Prototype a, Prototype b) =>
                {
                    return GetName(a.Method).CompareTo(GetName(b.Method));
                };
            }
        }

        public readonly struct Prototype
        {
            public MethodInfo Method { get; }
            public NetworkRPCAttribute Attribute { get; }

            public Prototype(MethodInfo method, NetworkRPCAttribute attribute)
            {
                this.Method = method;
                this.Attribute = attribute;
            }
        }
    }

    [Preserve]
    public abstract class RpcBind<TDelegate> : RpcBind
        where TDelegate : Delegate
    {
        public override void Configure(NetworkEntity.Behaviour behaviour, NetworkRPCAttribute attribute, MethodInfo method, byte index)
        {
            ID = new RpcID(index);
            this.Behaviour = behaviour;
            this.Attribute = attribute;

            //Method
            {
                var info = method;

                this.Method = method.CreateDelegate<TDelegate>(Behaviour.Component);

                Name = GetName(info);
                ReturnType = info.ReturnType;
            }
        }

        public TDelegate Method { get; private set; }

        public virtual void ProcessReturnValue<TReturn>(TReturn result)
        {
            if (result is IEnumerator routine)
                Component.StartCoroutine(routine);
        }
    }

    #region Void
    [Preserve]
    public class RpcBindVoid : RpcBind<Action<RpcInfo>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            Method.Invoke(info);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }

    [Preserve]
    public class RpcBindVoid<T1> : RpcBind<Action<T1, RpcInfo>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            var arg1 = reader.Read<T1>();

            Method.Invoke(arg1, info);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }

    [Preserve]
    public class RpcBindVoid<T1, T2> : RpcBind<Action<T1, T2, RpcInfo>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            var arg1 = reader.Read<T1>();
            var arg2 = reader.Read<T2>();

            Method.Invoke(arg1, arg2, info);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }

    [Preserve]
    public class RpcBindVoid<T1, T2, T3> : RpcBind<Action<T1, T2, T3, RpcInfo>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            var arg1 = reader.Read<T1>();
            var arg2 = reader.Read<T2>();
            var arg3 = reader.Read<T3>();

            Method.Invoke(arg1, arg2, arg3, info);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }

    [Preserve]
    public class RpcBindVoid<T1, T2, T3, T4> : RpcBind<Action<T1, T2, T3, T4, RpcInfo>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            var arg1 = reader.Read<T1>();
            var arg2 = reader.Read<T2>();
            var arg3 = reader.Read<T3>();
            var arg4 = reader.Read<T4>();

            Method.Invoke(arg1, arg2, arg3, arg4, info);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }

    [Preserve]
    public class RpcBindVoid<T1, T2, T3, T4, T5> : RpcBind<Action<T1, T2, T3, T4, T5, RpcInfo>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            var arg1 = reader.Read<T1>();
            var arg2 = reader.Read<T2>();
            var arg3 = reader.Read<T3>();
            var arg4 = reader.Read<T4>();
            var arg5 = reader.Read<T5>();

            Method.Invoke(arg1, arg2, arg3, arg4, arg5, info);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }

    [Preserve]
    public class RpcBindVoid<T1, T2, T3, T4, T5, T6> : RpcBind<Action<T1, T2, T3, T4, T5, T6, RpcInfo>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            var arg1 = reader.Read<T1>();
            var arg2 = reader.Read<T2>();
            var arg3 = reader.Read<T3>();
            var arg4 = reader.Read<T4>();
            var arg5 = reader.Read<T5>();
            var arg6 = reader.Read<T6>();

            Method.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, info);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }
    #endregion

    #region Return
    [Preserve]
    public class RpcBindReturn<TReturn> : RpcBind<Func<RpcInfo, TReturn>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            var result = Method.Invoke(info);
            ProcessReturnValue(result);
            if (writer != null) writer.Write(result);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }

    [Preserve]
    public class RpcBindReturn<T1, TReturn> : RpcBind<Func<T1, RpcInfo, TReturn>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            var arg1 = reader.Read<T1>();

            var result = Method.Invoke(arg1, info);
            ProcessReturnValue(result);
            writer.Write(result);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }

    [Preserve]
    public class RpcBindReturn<T1, T2, TReturn> : RpcBind<Func<T1, T2, RpcInfo, TReturn>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            var arg1 = reader.Read<T1>();
            var arg2 = reader.Read<T2>();

            var result = Method.Invoke(arg1, arg2, info);
            ProcessReturnValue(result);
            if (writer != null) writer.Write(result);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }

    [Preserve]
    public class RpcBindReturn<T1, T2, T3, TReturn> : RpcBind<Func<T1, T2, T3, RpcInfo, TReturn>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            var arg1 = reader.Read<T1>();
            var arg2 = reader.Read<T2>();
            var arg3 = reader.Read<T3>();

            var result = Method.Invoke(arg1, arg2, arg3, info);
            ProcessReturnValue(result);
            if (writer != null) writer.Write(result);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }

    [Preserve]
    public class RpcBindReturn<T1, T2, T3, T4, TReturn> : RpcBind<Func<T1, T2, T3, T4, RpcInfo, TReturn>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            var arg1 = reader.Read<T1>();
            var arg2 = reader.Read<T2>();
            var arg3 = reader.Read<T3>();
            var arg4 = reader.Read<T4>();

            var result = Method.Invoke(arg1, arg2, arg3, arg4, info);
            ProcessReturnValue(result);
            if (writer != null) writer.Write(result);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }

    [Preserve]
    public class RpcBindReturn<T1, T2, T3, T4, T5, TReturn> : RpcBind<Func<T1, T2, T3, T4, T5, RpcInfo, TReturn>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            var arg1 = reader.Read<T1>();
            var arg2 = reader.Read<T2>();
            var arg3 = reader.Read<T3>();
            var arg4 = reader.Read<T4>();
            var arg5 = reader.Read<T5>();

            var result = Method.Invoke(arg1, arg2, arg3, arg4, arg5, info);
            ProcessReturnValue(result);
            if (writer != null) writer.Write(result);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }

    [Preserve]
    public class RpcBindReturn<T1, T2, T3, T4, T5, T6, TReturn> : RpcBind<Func<T1, T2, T3, T4, T5, T6, RpcInfo, TReturn>>
    {
        public override void Invoke(NetworkReader reader, NetworkWriter writer, RpcInfo info)
        {
            var arg1 = reader.Read<T1>();
            var arg2 = reader.Read<T2>();
            var arg3 = reader.Read<T3>();
            var arg4 = reader.Read<T4>();
            var arg5 = reader.Read<T5>();
            var arg6 = reader.Read<T6>();

            var result = Method.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, info);
            ProcessReturnValue(result);
            if (writer != null) writer.Write(result);
        }

        //Static Utiltiy
        [Preserve]
        public static void Register()
        {

        }
    }
    #endregion

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