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
    public class SimpleNetworkTransform : NetworkBehaviour
    {
        Vector3 position;
        Quaternion rotation;

        void Update()
        {
            if(NetworkAPI.Client.IsMaster)
            {
                if(Time.frameCount % 1 == 0)
                {
                    var distance = Vector3.Distance(transform.position, position);

                    var angles = Quaternion.Angle(transform.rotation, rotation);

                    if (distance > 0.05f || angles > 0.05f)
                    {
                        rotation = transform.rotation;

                        position = transform.position;

                        RequestRPC(Sync, RpcBufferMode.Last, position, rotation);
                    }
                }
            }
        }

        [NetworkRPC]
        void Sync(Vector3 position, Quaternion rotation, RpcInfo info)
        {
            if (NetworkAPI.Client.IsMaster) return;

            transform.position = position;
            transform.rotation = rotation;
        }
	}
}