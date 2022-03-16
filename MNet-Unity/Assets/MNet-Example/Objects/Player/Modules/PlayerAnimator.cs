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
	public class PlayerAnimator : MonoBehaviour
	{
		Player Player;
		public void Set(Player reference) => Player = reference;

		public float Speed => Player.Movement.Speed;

		public SimpleNetworkAnimator NetworkAnimator => Player.NetworkAnimator;

		bool toggle = true;

		void Update()
		{
			if (Player.Entity.IsMine == false) return;

			if (Input.GetKeyDown(KeyCode.G)) toggle = !toggle;

			if (toggle == false) return;

			var velocity = Vector3.Scale(Player.rigidbody.velocity, Vector3.forward + Vector3.right);
			NetworkAnimator.SetFloat("Move", velocity.magnitude / Speed * 2);
		}
	}
}