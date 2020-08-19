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
	public class Player : NetworkBehaviour
	{
        void Start()
        {
            RequestRPC(RpcCall, 42, "Hello!", DateTime.Now);
        }

        void Update()
        {
            
        }

		[NetworkRPC]
        void RpcCall(int number, string text, DateTime date)
        {
            Debug.Log($"RPC Call! {number}, {text}, {date}");
        }
	}
}