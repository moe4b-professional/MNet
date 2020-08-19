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

namespace Game
{
	public class Player : NetworkBehaviour
	{
        public float speed;

        void Start()
        {
            if(IsMine)
            {
                var x = Random.Range(-5f, 5f);
                var z = Random.Range(-3f, 3f);

                transform.position = new Vector3(x, 0, z);
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

                RequestRPC(RpcSetPosition, transform.position + translation);
            }
        }

		[NetworkRPC]
        void RpcSetPosition(Vector3 position)
        {
            transform.position = position;
        }
	}
}