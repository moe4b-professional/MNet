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
	public class PlayerMovement : NetworkBehaviour
	{
		[SerializeField]
		float speed = 3.5f;

		[SerializeField]
		float acceleration = 20f;

		public Player Player { get; protected set; }
		public void Set(Player reference) => Player = reference;

        protected override void OnSpawn()
        {
            base.OnSpawn();

			Player.rigidbody.isKinematic = IsMine == false;

			if (IsMine) StartCoroutine(Broadcast());
        }

        void Update()
		{
			if (IsMine == false) return;

			var target = new Vector3()
			{
				x = Input.GetAxisRaw("Horizontal"),
				z = Input.GetAxisRaw("Vertical"),
			};

			target = Vector3.ClampMagnitude(target * speed, speed);

			var velocity = Vector3.Scale(Player.Velocity, Vector3.forward + Vector3.right);

			velocity = Vector3.MoveTowards(velocity, target, acceleration * Time.deltaTime);

			Player.Velocity = velocity + (Vector3.up * Player.Velocity.y);
		}

		IEnumerator Broadcast()
        {
			while(true)
            {
				if (IsConnected) BroadcastRPC(SyncCoordinates, transform.position, buffer: RpcBufferMode.Last, exception: NetworkAPI.Client.ID);

				yield return new WaitForSeconds(0.1f);
			}
		}

		[NetworkRPC(Authority = RemoteAuthority.Owner, Delivery = DeliveryMode.Unreliable)]
		void SyncCoordinates(Vector3 position, RpcInfo info)
        {
			Player.Position = position;
        }
	}
}