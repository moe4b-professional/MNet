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

namespace Game
{
    [DefaultExecutionOrder(ExecutionOrder)]
	public class NetworkEntity : MonoBehaviour
	{
        public const int ExecutionOrder = -200;

        public NetworkClientID Owner { get; protected set; }

        public NetworkEntityID ID { get; protected set; }

        public bool IsMine => Owner == NetworkClient.ID;

        public Dictionary<NetworkBehaviourID, NetworkBehaviour> Behaviours { get; protected set; }

        protected virtual void Awake()
        {
            Behaviours = new Dictionary<NetworkBehaviourID, NetworkBehaviour>();

            var targets = GetComponentsInChildren<NetworkBehaviour>();

            if(targets.Length > byte.MaxValue)
                throw new Exception($"Entity {name} May Only Have Up To {byte.MaxValue} Behaviours, Current Count: {targets.Length}");

            var count = (byte)targets.Length;

            for (byte i = 0; i < count; i++)
            {
                Behaviours.Add(targets[i].ID, targets[i]);

                var id = new NetworkBehaviourID(i);

                targets[i].Set(this, id);
            }
        }

        public void Spawn(NetworkClientID owner, NetworkEntityID id)
        {
            this.Owner = owner;
            this.ID = id;
        }

        public void InvokeRpc(RpcCommand command)
        {
            if (Behaviours.TryGetValue(command.Behaviour, out var target))
                target.InvokeRpc(command);
            else
                Debug.LogWarning($"No Behaviour with ID {command.Behaviour} found to invoke RPC");
        }
    }
}