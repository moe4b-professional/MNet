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
	public class LatencyTest : NetworkBehaviour
	{
        void Update()
        {
            if (IsMine && Input.GetKey(KeyCode.Mouse0) && timestamp == null)
            {
                timestamp = Time.time;

                RequestRPC(Click, Owner, transform.position, transform.rotation);
            }
        }

        float? timestamp;

        [NetworkRPC]
        void Click(Vector3 position, Quaternion rotation, RpcInfo info)
        {
            var elapsed = Time.time - timestamp;

            Debug.Log($"RPC Latency: {elapsed * 1000}ms");

            timestamp = null;
        }
    }
}