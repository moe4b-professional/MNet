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
	public class PlayerAnimator : NetworkBehaviour
	{
		Player player;
		public void Set(Player reference) => player = reference;

		public float Speed => player.Movement.Speed;

		public SimpleNetworkAnimator NetworkAnimator => player.NetworkAnimator;

		void Update()
		{
			if (Entity.IsMine == false) return;

			var velocity = Vector3.Scale(player.rigidbody.velocity, Vector3.forward + Vector3.right);
			NetworkAnimator.SetFloat("Move", velocity.magnitude / Speed * 2);
		}
	}
}