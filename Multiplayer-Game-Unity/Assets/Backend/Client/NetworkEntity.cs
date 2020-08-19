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
        [ReadOnly]
        [SerializeField]
        protected string _ID = string.Empty;
        public string ID
        {
            get => _ID;
            set => _ID = value;
        }

        [ReadOnly]
        [SerializeField]
        string owner;
        public string Owner => owner;

        public bool IsMine => owner == NetworkClient.ID;

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

        public void Spawn(string owner, string id)
        {
            this.owner = owner;
            this.ID = id;
        }

        public void InvokeRpc(RpcPayload payload)
        {
            if (Behaviours.TryGetValue(payload.Behaviour, out var target))
                target.InvokeRpc(payload);
            else
                Debug.LogWarning($"No Behaviour with ID {payload.Behaviour} found to invoke RPC");
        }
    }
}