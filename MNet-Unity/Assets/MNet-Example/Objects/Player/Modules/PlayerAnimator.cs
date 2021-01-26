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
		[SerializeField]
		Animator animator = default;

		Player player;
		public void Set(Player reference) => player = reference;

		public Vector3 Velocity
        {
			get
            {
				if (Entity.IsMine)
					return player.rigidbody.velocity;
				else
					return player.NetworkTransform.Velocity.Vector;
            }
        }

		public float Speed => player.Movement.Speed;

		void Update()
		{
			animator.SetFloat("Move", Velocity.magnitude / Speed * 2);
		}
	}
}