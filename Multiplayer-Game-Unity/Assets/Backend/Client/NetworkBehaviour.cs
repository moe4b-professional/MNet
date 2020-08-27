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

namespace Backend
{
    [RequireComponent(typeof(NetworkEntity))]
    public partial class NetworkBehaviour : MonoBehaviour
	{
        public NetworkBehaviourID ID { get; protected set; }

        public NetworkEntity Entity { get; protected set; }

        public AttributesCollection Attributes => Entity?.Attributes;

        public void Configure(NetworkEntity entity, NetworkBehaviourID id)
        {
            Entity = entity;

            this.ID = id;

            RPCs = new RpcCollection(this);

            OnSpawn();
        }

        protected virtual void OnSpawn() { }

        public bool IsMine => Entity.IsMine;

        public RpcCollection RPCs { get; protected set; }

        protected void RequestRPC(MethodInfo method, params object[] arguments) => RequestRPC(method.Name, arguments);
        protected void RequestRPC(string name, params object[] arguments)
        {
            if (RPCs.Find(name, out var bind))
            {
                if (NetworkAPI.Client.IsConnected == false)
                {
                    Debug.LogWarning($"Cannot Send RPC {name} When Client Isn't Connected");
                    return;
                }

                var payload = bind.CreateRequest(arguments);

                NetworkAPI.Client.Send(payload);
            }
            else
                Debug.LogWarning($"No RPC Found With Name {name}");
        }

        public void InvokeRpc(RpcCommand command)
        {
            if(RPCs.Find(command.Method, out var bind))
                bind.InvokeCommand(command);
            else
                Debug.LogWarning($"No RPC with Name {command.Method} found on {GetType().Name}");
        }
    }
}