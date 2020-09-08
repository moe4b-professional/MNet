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

namespace Backend
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class NetworkEntity : MonoBehaviour
    {
        public const int ExecutionOrder = -200;

        public NetworkClient Owner { get; protected set; }

        public NetworkEntityID ID { get; protected set; }

        public bool IsMine => Owner?.ID == NetworkAPI.Client.ID;

        public AttributesCollection Attributes { get; protected set; }

        public Dictionary<NetworkBehaviourID, NetworkBehaviour> Behaviours { get; protected set; }

        public bool IsReady { get; protected set; } = false;

        public Scene Scene => gameObject.scene;

        protected virtual void Awake()
        {
            NetworkScene.Get(Scene)?.Register(this);
        }

        public void Configure(NetworkClient owner, NetworkEntityID id, AttributesCollection attributes)
        {
            IsReady = true;

            this.Owner = owner;
            this.ID = id;
            this.Attributes = attributes;

            RegisterBehaviours();

            OnSpawn();
        }

        void RegisterBehaviours()
        {
            Behaviours = new Dictionary<NetworkBehaviourID, NetworkBehaviour>();

            var targets = GetComponentsInChildren<NetworkBehaviour>();

            if (targets.Length > byte.MaxValue)
                throw new Exception($"Entity {name} May Only Have Up To {byte.MaxValue} Behaviours, Current Count: {targets.Length}");

            var count = (byte)targets.Length;

            for (byte i = 0; i < count; i++)
            {
                var id = new NetworkBehaviourID(i);

                targets[i].Configure(this, id);

                Behaviours.Add(id, targets[i]);
            }
        }

        public void InvokeRpc(RpcCommand command)
        {
            if (Behaviours.TryGetValue(command.Behaviour, out var target))
                target.InvokeRPC(command);
            else
                Debug.LogWarning($"No Behaviour with ID {command.Behaviour} found to invoke RPC");
        }

        protected virtual void OnSpawn()
        {

        }

        protected virtual void OnDestroy()
        {
            NetworkScene.Get(Scene)?.Remove(this);
        }
    }
}