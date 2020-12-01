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

        float timer = 0f;

        void Update()
        {
            timer = Mathf.MoveTowards(timer, delay, Time.deltaTime);

            if (Mathf.Approximately(timer, delay)) Request();
        }

        void Request() => BroadcastRPC(Call, transform.position, transform.rotation);

        [NetworkRPC(Delivery = DeliveryMode.Unreliable)]
        void Call(Vector3 position, Quaternion rotation, RpcInfo info)
        {
            pakcets += 1;
        }
    }
}