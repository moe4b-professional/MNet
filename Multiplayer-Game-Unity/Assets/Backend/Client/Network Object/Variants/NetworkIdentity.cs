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

namespace Game
{
    [DefaultExecutionOrder(-100)]
	public class NetworkIdentity : NetworkObject
	{
        public NetworkBehaviour[] Behaviours { get; protected set; }

        protected override void Awake()
        {
            base.Awake();

            Behaviours = GetComponentsInChildren<NetworkBehaviour>();

            for (int i = 0; i < Behaviours.Length; i++)
                Behaviours[i].Set(this);
        }

        public void Spawn(string ID)
        {
            this.ID = ID;
        }
    }
}