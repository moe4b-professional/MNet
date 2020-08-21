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
    [DefaultExecutionOrder(-100)]
	public class NetworkEntity : MonoBehaviour
	{
        public NetworkClientID Owner { get; protected set; }

        public NetworkEntityID ID { get; protected set; }

        public bool IsMine => Owner == NetworkClient.ID;

        public Dictionary<string, NetworkBehaviour> Behaviours { get; protected set; }

        protected virtual void Awake()
        {
            var list = GetComponentsInChildren<NetworkBehaviour>();

            Behaviours = new Dictionary<string, NetworkBehaviour>();

            for (int i = 0; i < list.Length; i++)
            {
                Behaviours.Add(list[i].ID, list[i]);

                list[i].Set(this);
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