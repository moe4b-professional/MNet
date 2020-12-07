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
    public class SampleNetworkBehaviour : NetworkBehaviour
    {
        public float delay = 0.05f;

        public int pakcets = 0;

        void ReadAttributes(out Vector3 position, out Quaternion rotation)
        {
            Attributes.TryGetValue(0, out position);
            Attributes.TryGetValue(1, out rotation);
        }

        void Start()
        {
            ReadAttributes(out var position, out var rotation);

            transform.position = position;
            transform.rotation = rotation;

            StartCoroutine(Procedure());
        }

        IEnumerator Procedure()
        {
            while(true)
            {
                Request();

                yield return new WaitForSeconds(delay);
            }
        }

        void Request() => BroadcastRPC(Call, transform.position, transform.rotation);

        [NetworkRPC(Delivery = DeliveryMode.Unreliable)]
        void Call(Vector3 position, Quaternion rotation, RpcInfo info)
        {
            pakcets += 1;
        }
    }
}