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
		public float Speed => speed;

		[SerializeField]
		float acceleration = 20f;
		public float Acceleration => acceleration;

		Player player;
		public void Set(Player reference) => player = reference;

		public NetworkEntity Entity => player.Entity;
		public PlayerRotation Rotation => player.Rotation;

		public Vector3 PlannarVelocity
		{
			get => Vector3.Scale(player.rigidbody.velocity, Vector3.forward + Vector3.right);
			set
            {
				value.y = player.rigidbody.velocity.y;

				player.rigidbody.velocity = value;
			}
		}

		void Update()
		{
			if (Entity.IsOrphan) return;
			if (Entity.IsReady == false) return;
			if (Entity.IsMine == false) return;

			var target = new Vector3()
			{
				x = Input.GetAxisRaw("Horizontal"),
				z = Input.GetAxisRaw("Vertical"),
			};

			var yDelta = Rotation.Process(PlannarVelocity);
			var yScale = Mathf.Lerp(1f, 0.2f, yDelta / 120);

			target = Vector3.ClampMagnitude(target * speed * yScale, speed);

			PlannarVelocity = Vector3.MoveTowards(PlannarVelocity, target, acceleration * Time.deltaTime);
		}
	}
}