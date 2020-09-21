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
        void Start()
        {
			if (IsMine == false) return;

			RequestRPC("Rpc", Entity, this);
		}

        [NetworkRPC]
		void Rpc(NetworkEntity entity, SerializationTest behaviour, RpcInfo info)
        {
			Debug.Log($"Entity: {entity}");
			Debug.Log($"Behaviour: {behaviour}");
		}
	}
}