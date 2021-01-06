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

		string var = default;
		[SyncVar(Authority = RemoteAuthority.Owner)]
		public string Var
		{
			get => var;
			set
			{
				var = value;
			}
		}

		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void Call(string arg, RpcInfo infp)
		{

		}

		void Start()
		{
			if (Entity.IsMine) StartCoroutine(Procedure());
		}

		IEnumerator Procedure()
		{
			while (Entity.IsConnected)
			{
				SyncVar("Var", Var, "Hello World");
				BroadcastRPC(Call, "Hello World");

				yield return new WaitForSeconds(interval);
			}
		}
	}
}