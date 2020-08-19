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
	public class NetworkEntity : NetworkObject
	{
        public NetworkBehaviour[] Behaviours { get; protected set; }

        public Dictionary<string, NetworkBehaviour> Dictionary { get; protected set; }

        protected override void Awake()
        {
            base.Awake();

            Behaviours = GetComponentsInChildren<NetworkBehaviour>();

            Dictionary = new Dictionary<string, NetworkBehaviour>();

            for (int i = 0; i < Behaviours.Length; i++)
            {
                Dictionary.Add(Behaviours[i].ID, Behaviours[i]);

                Behaviours[i].Set(this);
            }
        }

        public void Spawn(string ID)
        {
            this.ID = ID;
        }

        public void InvokeRpc(RpcPayload payload)
        {
            if(Dictionary.TryGetValue(payload.Behaviour, out var behaviour))
            {
                behaviour.InvokeRpc(payload);
            }
            else
            {
                Debug.LogWarning($"No Behaviour with ID {payload.Behaviour} found to invoke RPC");
            }
        }
    }
}