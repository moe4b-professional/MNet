﻿using System;
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

        public bool HasReturn => MethodInfo.ReturnType != null;

        public RpcRequest CreateRequest(RpcBufferMode bufferMode, params object[] arguments)
        {
            var request = RpcRequest.Write(Entity.ID, Behaviour.ID, ID, bufferMode, arguments);

            return request;
        }
        public RpcRequest CreateRequest(NetworkClientID target, params object[] arguments)
        {
            var request = RpcRequest.Write(Entity.ID, Behaviour.ID, ID, target, arguments);

            return request;
        }
        public RpcRequest CreateRequest(NetworkClientID target, ushort callback, params object[] arguments)
        {
            var request = RpcRequest.Write(Entity.ID, Behaviour.ID, ID, target, callback, arguments);

            return request;
        }

        public object Invoke(RpcCommand command)
        {
            var arguments = command.Read(ParametersInfo, HasInfoParameter ? 1 : 0);

            if(HasInfoParameter)
            {
                NetworkAPI.Room.Clients.TryGetValue(command.Sender, out var sender);

                var info = new RpcInfo(sender);

                arguments[arguments.Length - 1] = info;
            }

            return Invoke(arguments);
        }
        public object Invoke(params object[] arguments)
        {
            return MethodInfo.Invoke(Behaviour, arguments);
        }

        public RpcBind(NetworkBehaviour behaviour, NetworkRPCAttribute attribute, MethodInfo method)
        {
            Behaviour = behaviour;

            Attribute = attribute;

            MethodInfo = method;
            ID = MethodInfo.Name;

            ParametersInfo = method.GetParameters();
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
        /// <summary>
        /// As the name implies, any client will be able to invoke this RPC
        /// </summary>
        Any,

        /// <summary>
        /// Only the owner of this entity may invoke this RPC
        /// </summary>
        Owner,

        /// <summary>
        /// Only the master client may invoke this RPC
        /// </summary>
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

    public class RpcBindCollection
    {
        public Dictionary<string, RpcBind> Dictionary { get; protected set; }

        public BindingFlags BindingFlags => BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        public bool Find(string name, out RpcBind bind)
        {
            return Dictionary.TryGetValue(name, out bind);
        }

        public RpcBindCollection(NetworkBehaviour behaviour)
        {
            Dictionary = new Dictionary<string, RpcBind>();

            var type = behaviour.GetType();

            foreach (var method in type.GetMethods(BindingFlags))
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

    public class RpcCallbackBind
    {
        public ushort ID { get; protected set; }

        public object Target { get; protected set; }

        public MethodInfo Method { get; protected set; }
        public ParameterInfo[] Parameters { get; protected set; }

        public Type Type => Parameters[0].ParameterType;

        public void Invoke(params object[] arguments) => Method.Invoke(Target, arguments);

        public RpcCallbackBind(ushort id, MethodInfo method, object target)
        {
            this.ID = id;
            this.Target = target;
            this.Method = method;

            Parameters = method.GetParameters();
        }
    }

    public delegate void RpcMethod(RpcInfo info);
    public delegate void RpcMethod<T1>(T1 arg1, RpcInfo info);
    public delegate void RpcMethod<T1, T2>(T1 arg1, T2 arg2, RpcInfo info);
    public delegate void RpcMethod<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, RpcInfo info);
    public delegate void RpcMethod<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, RpcInfo info);
    public delegate void RpcMethod<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RpcInfo info);
    public delegate void RpcMethod<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RpcInfo info);

    public delegate TResult RpcCallbackMethod<TResult>(RpcInfo info);
    public delegate TResult RpcCallbackMethod<TResult, T1>(T1 arg1, RpcInfo info);
    public delegate TResult RpcCallbackMethod<TResult, T1, T2>(T1 arg1, T2 arg2, RpcInfo info);
    public delegate TResult RpcCallbackMethod<TResult, T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, RpcInfo info);
    public delegate TResult RpcCallbackMethod<TResult, T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, RpcInfo info);
    public delegate TResult RpcCallbackMethod<TResult, T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, RpcInfo info);
    public delegate TResult RpcCallbackMethod<TResult, T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, RpcInfo info);
}