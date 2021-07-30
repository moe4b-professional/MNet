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

namespace MNet.Example
{
	public class StressBehaviour : NetworkBehaviour
	{
		public float interval = 0.1f;

		[SerializeField]
		SyncVar<string> syncvar = SyncVar.From(string.Empty, RemoteAuthority.Owner);

		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void Call(string arg, RpcInfo infp)
		{

		}

        public override void OnNetwork()
        {
            base.OnNetwork();

			Network.OnSpawn += SpawnCallback;
        }

        void SpawnCallback()
        {
			if (Entity.IsMine) Debug.LogWarning($"Stress Behaviour Attached to {Entity}, Don't Forget to Remove it when not Stressing");

			StartCoroutine(Procedure());
		}

        IEnumerator Procedure()
		{
			while (Entity.IsConnected)
			{
				if (Entity.IsMine)
				{
					syncvar.Sync("Hello World").Broadcast();
					Network.BroadcastRPC(Call, "Hello World").Send();
				}

				yield return new WaitForSeconds(interval);
			}
		}
	}
}