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

namespace MNet
{
    [AddComponentMenu(NetworkAPI.Path + "Tests/" + nameof(LatencyTest))]
	public class LatencyTest : NetworkBehaviour
	{
        string payload = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

        void Update()
        {
            if (IsMine && Input.GetKey(KeyCode.Mouse0) && timestamp == null)
            {
                timestamp = Time.time;

                RPC(Click, Owner, payload);
            }
        }

        float? timestamp;

        [NetworkRPC]
        void Click(string payload, RpcInfo info)
        {
            var elapsed = Time.time - timestamp;

            Debug.Log($"RPC Latency: {elapsed * 1000}ms");

            timestamp = null;
        }
    }
}