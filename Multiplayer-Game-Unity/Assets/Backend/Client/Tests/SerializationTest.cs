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

using Backend;

namespace Game
{
	public class SerializationTest : NetworkBehaviour
	{
		AttributesCollection attribute;

        void Start()
        {
			if (IsMine == false) return;

			RequestRPC(Rpc, Entity, this, Color.red);
		}

        [NetworkRPC]
		void Rpc(NetworkEntity entity, SerializationTest behaviour, Color color, RpcInfo info)
        {
			Debug.Log($"Entity: {entity}");
			Debug.Log($"Behaviour: {behaviour}");
			Debug.Log($"Color: {color}");
		}
	}
}