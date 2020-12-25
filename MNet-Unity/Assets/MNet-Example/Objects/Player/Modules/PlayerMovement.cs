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
	public class PlayerMovement : MonoBehaviour
	{
		[SerializeField]
		float speed = 3.5f;

		[SerializeField]
		float acceleration = 20f;

		public Player Player { get; protected set; }
		public void Set(Player reference) => Player = reference;

		public PlayerRotation Rotation => Player.Rotation;

		void Update()
		{
			if (Player.IsMine == false) return;

			var target = new Vector3()
			{
				x = Input.GetAxisRaw("Horizontal"),
				z = Input.GetAxisRaw("Vertical"),
			};

			target = Vector3.ClampMagnitude(target * speed, speed);

			var velocity = Vector3.Scale(Player.Velocity, Vector3.forward + Vector3.right);

			velocity = Vector3.MoveTowards(velocity, target, acceleration * Time.deltaTime);

			Player.Velocity = velocity + (Vector3.up * Player.Velocity.y);

			Rotation.Process(velocity);
		}
	}
}