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

using Backend;

namespace Game
{
	public class Player : NetworkBehaviour
	{
        new public Renderer renderer;

        public float speed;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if (Attributes != null)
            {
                if (Attributes.TryGetValue("Position", out Vector3 position))
                    transform.position = position;

                if (Attributes.TryGetValue("Rotation", out Quaternion rotation))
                    transform.rotation = rotation;
            }
        }

        protected override void Start()
        {
            base.Start();

            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", IsMine ? Color.green : Color.red);
            renderer.SetPropertyBlock(block);
        }

        void Update()
        {
            if (IsMine)
            {
                var input = new Vector2()
                {
                    x = Input.GetAxisRaw("Horizontal"),
                    y = Input.GetAxisRaw("Vertical"),
                };

                if (input.magnitude > 0.1f) RequestRPC(RequestMove, NetworkAPI.Room.Master, input);
            }
        }

        [NetworkRPC(RpcAuthority.Owner)]
        void RequestMove(Vector2 input, RpcInfo info)
        {
            input = Vector2.ClampMagnitude(input, 1f);

            var direction = new Vector3(input.x, 0f, input.y);

            var velocity = Vector3.ClampMagnitude(direction * speed, speed);

            var position = transform.position + (velocity * Time.deltaTime);
            var rotation = velocity.magnitude > 0.1f ? Quaternion.LookRotation(velocity) : transform.rotation;

            RequestRPC(SetCoordinates, RpcBufferMode.Last, position, rotation);
        }

        [NetworkRPC(RpcAuthority.Master)]
        void SetCoordinates(Vector3 position, Quaternion rotation, RpcInfo info)
        {
            transform.position = position;
            transform.rotation = rotation;
        }
    }
}