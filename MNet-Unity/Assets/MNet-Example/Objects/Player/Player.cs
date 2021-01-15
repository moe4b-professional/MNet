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

			Rotation = GetComponentInChildren<PlayerRotation>();
			Rotation.Set(this);
		}

		protected override void OnSetup()
		{
			base.OnSetup();

			ReadAttributes(Entity.Attributes, out var position, out var rotation);

			transform.position = position;
			transform.rotation = rotation;

			transform.position += Vector3.up * 0.9f;
		}

		protected override void OnOwnerSet(NetworkClient client)
		{
			base.OnOwnerSet(client);

			rigidbody.isKinematic = Entity.IsMine == false;
		}

		//Static Utility

		public static NetworkEntity Spawn() => Spawn(Vector3.zero, Quaternion.identity);
		public static NetworkEntity Spawn(Vector3 position, Quaternion rotation)
		{
			var attributes = WriteAttributes(position, rotation);

			var persistance = PersistanceFlags.SceneLoad;

			return NetworkAPI.Client.Entities.Spawn("Player", attributes: attributes, persistance: persistance);
		}

		public static AttributesCollection WriteAttributes(Vector3 position, Quaternion rotation)
		{
			var collection = new AttributesCollection();

			collection.Set(0, position);
			collection.Set(1, rotation);

			return collection;
		}

		public static void ReadAttributes(AttributesCollection collection, out Vector3 position, out Quaternion rotation)
		{
			collection.TryGetValue(0, out position);
			collection.TryGetValue(1, out rotation);
		}
	}
#pragma warning restore CS0108
}