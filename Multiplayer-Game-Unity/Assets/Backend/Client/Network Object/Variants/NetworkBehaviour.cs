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

using Game.Shared;

using System.Reflection;

namespace Game
{
    [RequireComponent(typeof(NetworkEntity))]
    public class NetworkBehaviour : NetworkObject
	{
        public NetworkEntity Identity { get; protected set; }
        public void Set(NetworkEntity reference)
        {
            Identity = reference;
        }

        public RpcCollection RPCs { get; protected set; }
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
                    var attribute = method.GetCustomAttribute(typeof(NetworkRPCAttribute));

                    if (attribute == null) continue;

                    var bind = new RpcBind(behaviour, method);

                    Dictionary.Add(bind.Name, bind);
                }
            }
        }

        public void GenerateID()
        {
            ID = Guid.NewGuid().ToString("N");
        }

        protected virtual void Reset()
        {
            GenerateID();
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(ID)) GenerateID();
        }

        protected override void Awake()
        {
            base.Awake();

            RPCs = new RpcCollection(this);
        }

        #region Request RPC
        public void RequestRPC(RpcCallback callback)
            => RequestRPC(callback.Method);
        public void RequestRPC<T1>(RpcCallback<T1> callback, T1 arg1)
            => RequestRPC(callback.Method, arg1);
        public void RequestRPC<T1, T2>(RpcCallback<T1, T2> callback, T1 arg1, T2 arg2)
            => RequestRPC(callback.Method, arg1, arg2);
        public void RequestRPC<T1, T2, T3>(RpcCallback<T1, T2, T3> callback, T1 arg1, T2 arg2, T3 arg3)
            => RequestRPC(callback.Method, arg1, arg2, arg3);

        protected void RequestRPC(MethodInfo method, params object[] parameters) => RequestRPC(method.Name, parameters);
        protected void RequestRPC(string name, params object[] parameters)
        {
            if (RPCs.Find(name, out var bind))
            {
                var payload = bind.Request(parameters);

                var message = NetworkMessage.Write(payload);

                NetworkClient.Room.Send(message);
            }
        }
        #endregion

        public void InvokeRpc(RpcPayload payload)
        {
            if(RPCs.Find(payload.Method, out var bind))
            {
                bind.Invoke(payload);
            }
            else
            {
                Debug.Log($"No RPC with Name {payload.Method} found on {GetType().Name}");
            }
        }
    }
}