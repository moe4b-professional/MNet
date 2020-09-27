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

			attribute = new AttributesCollection();

			attribute.Set(0, Color.red);
			attribute.Set(1, Vector2Int.one);
			attribute.Set(2, Vector3.up);
			attribute.Set(3, this);
			attribute.Set(4, Entity);

            foreach (var key in attribute.Keys) Debug.Log(attribute[key]);

			Debug.Log(attribute.TryGetValue<SerializationTest>(3, out var value));
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