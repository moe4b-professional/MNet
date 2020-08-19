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
    [RequireComponent(typeof(NetworkIdentity))]
    public class NetworkBehaviour : NetworkObject
	{
        public NetworkIdentity Identity { get; protected set; }
        public void Set(NetworkIdentity reference)
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

        public void RequestRPC(RpcCallback callback)
        {
            if(RPCs.Find(callback.Method.Name, out var bind))
            {
                var message = bind.Request().ToMessage();

                NetworkClient.Room.Send(message);
            }
        }
        public void RequestRPC<T1>(RpcCallback<T1> callback, T1 arg1)
        {
            if (RPCs.Find(callback.Method.Name, out var bind))
            {
                var message = bind.Request(arg1).ToMessage();

                NetworkClient.Room.Send(message);
            }
        }
        public void RequestRPC<T1, T2>(RpcCallback<T1, T2> callback, T1 arg1, T2 arg2)
        {
            if (RPCs.Find(callback.Method.Name, out var bind))
            {
                var message = bind.Request(arg1, arg2).ToMessage();

                NetworkClient.Room.Send(message);
            }
        }
        public void RequestRPC<T1, T2, T3>(RpcCallback<T1, T2, T3> callback, T1 arg1, T2 arg2, T3 arg3)
        {
            if (RPCs.Find(callback.Method.Name, out var bind))
            {
                var message = bind.Request(arg1, arg2, arg3).ToMessage();

                NetworkClient.Room.Send(message);
            }
        }

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