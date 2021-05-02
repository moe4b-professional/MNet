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

namespace MNet
{
	[AddComponentMenu(Constants.Path + "Tests/" + "Serialization Test")]
	public class SerializationTest : NetworkBehaviour
	{
		public bool success = false;

        public override void OnNetwork()
        {
            base.OnNetwork();

			Network.OnSpawn += SpawnCallback;
        }

        void SpawnCallback()
		{
			Network.TargetRPC(Rpc, NetworkAPI.Client.Self, Entity, this);
		}

		[NetworkRPC]
		void Rpc(NetworkEntity entity, SerializationTest behaviour, RpcInfo info)
		{
			success = Network.Entity == entity && this == behaviour;
		}
	}
}