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
	public class Player : Actor
	{
		public PlayerMovement Movement { get; protected set; }
		public PlayerRotation Rotation { get; protected set; }
		public PlayerAnimator Animator { get; protected set; }

		public Rigidbody rigidbody { get; protected set; }

		public Vector3 Position
		{
			get => transform.position;
			set => transform.position = value;
		}

		public SimpleNetworkTransform NetworkTransform { get; protected set; }
		public SimpleNetworkAnimator NetworkAnimator { get; protected set; }

        public override void OnNetwork()
        {
            base.OnNetwork();

            Network.OnSetup += SetupCallback;
            Network.OnOwnerSet += OwnerSetCallback;
        }

		void SetupCallback()
		{
			ReadAttributes(Entity, out var position, out var rotation);

			transform.position = position;
			transform.rotation = rotation;

			transform.position += Vector3.up * 0.9f;
		}

		void OwnerSetCallback(NetworkClient client)
        {
			rigidbody.isKinematic = Entity.IsMine == false;
		}

		void Awake()
		{
			rigidbody = GetComponent<Rigidbody>();

			NetworkTransform = GetComponent<SimpleNetworkTransform>();
			NetworkAnimator = GetComponentInChildren<SimpleNetworkAnimator>();

			Movement = GetComponentInChildren<PlayerMovement>();
			Movement.Set(this);

			Rotation = GetComponentInChildren<PlayerRotation>();
			Rotation.Set(this);

			Animator = GetComponentInChildren<PlayerAnimator>();
			Animator.Set(this);
		}

		//Static Utility

		public static NetworkEntity Spawn(GameObject prefab, float range)
        {
			var position = new Vector3
			{
				x = Random.Range(-range, range),
				y = 0f,
				z = Random.Range(-range, range),
			};

			var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

			var color = Random.ColorHSV();

			return Spawn(prefab, position, rotation, color);
        }
		public static NetworkEntity Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Color color)
		{
			var attributes = WriteAttributes(position, rotation);

			PlayerMesh.WriteAttributes(attributes, color);

			var persistance = PersistanceFlags.SceneLoad;

			return NetworkAPI.Client.Entities.Spawn(prefab, attributes: attributes, persistance: persistance);
		}

		public static AttributesCollection WriteAttributes(Vector3 position, Quaternion rotation)
		{
			var collection = new AttributesCollection();

			collection.Set(0, position);
			collection.Set(1, rotation);

			return collection;
		}

		public static void ReadAttributes(NetworkEntity entity, out Vector3 position, out Quaternion rotation)
		{
			if(entity.Attributes == null)
            {
				Debug.LogWarning($"No Attributes Registered with Entity {entity}");
				position = default;
				rotation = default;
				return;
            }

			entity.Attributes.TryGetValue(0, out position);
			entity.Attributes.TryGetValue(1, out rotation);
		}
	}
#pragma warning restore CS0108
}