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

using Backend.Shared;

namespace Backend
{
	public class Player : NetworkBehaviour
	{
        public float speed;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if(Attributes != null)
            {
                if (Attributes.TryGetValue("Position", out Vector3 position))
                    transform.position = position;

                if (Attributes.TryGetValue("Rotation", out Quaternion rotation))
                    transform.rotation = rotation;
            }
        }

        void Update()
        {
            if(IsMine)
            {
                var horizontal = Input.GetAxisRaw("Horizontal");
                var vertical = Input.GetAxisRaw("Vertical");

                var direction = new Vector3(horizontal, 0f, vertical);

                var velocity = Vector3.ClampMagnitude(direction * speed, speed);

                var translation = velocity * Time.deltaTime;

                var rotation = translation.magnitude > 0.1f ? Quaternion.LookRotation(translation) : transform.rotation;

                RequestRPC(RpcSetPosition, transform.position + translation, rotation);
            }
        }

		[NetworkRPC(RpcBufferMode.Last)]
        void RpcSetPosition(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
        }
	}
}