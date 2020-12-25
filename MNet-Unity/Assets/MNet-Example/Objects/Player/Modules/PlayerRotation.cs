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
	public class PlayerRotation : MonoBehaviour
	{
		[SerializeField]
		float speed = 420; //Blaze it bruh !!!
		public float Speed => speed;

		public Player Player { get; protected set; }
		public void Set(Player reference) => Player = reference;

		public void Process(Vector3 velocity)
        {
			if (velocity.magnitude < 0.1f) return;

			var target = Vector2Angle(velocity.x, velocity.z);

			Player.yAngle = Mathf.MoveTowardsAngle(Player.yAngle, target, speed * Time.deltaTime);
		}

		public static float Vector2Angle(float x, float y)
		{
			return Mathf.Atan2(x, y) * Mathf.Rad2Deg;
		}
	}
}