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
	[RequireComponent(typeof(Rigidbody))]
#pragma warning disable CS0108
	public class Player : NetworkBehaviour
	{
		public PlayerMovement Movement { get; protected set; }

		public Rigidbody rigidbody { get; protected set; }

		public Vector3 Velocity
        {
			get => rigidbody.velocity;
			set => rigidbody.velocity = value;
        }

		public Vector3 Position
        {
			get => transform.position;
			set => transform.position = value;
        }

		public class Behaviour : NetworkBehaviour
        {
			public Player Player { get; protected set; }
			public void Set(Player reference) => Player = reference;
        }

		void Awake()
        {
			rigidbody = GetComponent<Rigidbody>();

			Movement = GetComponentInChildren<PlayerMovement>();
			Movement.Set(this);
        }

		void Start()
        {

        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

			rigidbody.isKinematic = IsMine == false;
		}
    }
#pragma warning restore CS0108
}