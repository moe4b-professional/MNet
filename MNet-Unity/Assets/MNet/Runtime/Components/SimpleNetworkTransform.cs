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
    [AddComponentMenu(Constants.Path + "Simple Network Transform")]
    public class SimpleNetworkTransform : NetworkBehaviour
    {
        Vector3 lastPosition;
        Quaternion lastRotation;

        void Update()
        {
            if (NetworkAPI.Client.IsMaster) Process();
        }

        void Process()
        {
            if (Time.frameCount % 4 != 0) return;

            if (DetectChange())
            {
                lastPosition = transform.position;
                lastRotation = transform.rotation;

                BroadcastRPC(Sync, lastPosition, lastRotation, buffer: RpcBufferMode.Last);
            }
        }

        bool DetectChange()
        {
            var distance = Vector3.Distance(transform.position, lastPosition);
            if (distance > 0.05f) return true;

            var angles = Quaternion.Angle(transform.rotation, lastRotation);
            if (angles > 0.05f) return true;

            return false;
        }

        [NetworkRPC(Authority = RemoteAuthority.Master)]
        void Sync(Vector3 position, Quaternion rotation, RpcInfo info)
        {
            if (NetworkAPI.Client.IsMaster)
            {
                if (info.IsBuffered == false) return;

                lastPosition = position;
                lastRotation = rotation;
            }

            transform.position = position;
            transform.rotation = rotation;
        }
	}
}