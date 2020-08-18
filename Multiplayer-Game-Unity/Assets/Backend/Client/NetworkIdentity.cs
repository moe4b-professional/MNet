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
	public class NetworkIdentity : MonoBehaviour
	{
		public NetworkBehaviour[] Behaviours { get; protected set; }

        void Awake()
        {
            Behaviours = GetComponentsInChildren<NetworkBehaviour>();

            for (int i = 0; i < Behaviours.Length; i++)
                Behaviours[i].Set(this);

            Debug.Log(GetInstanceID());
        }
    }
}