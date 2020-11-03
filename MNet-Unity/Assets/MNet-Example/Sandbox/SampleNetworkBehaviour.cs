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
        [SyncVar(RemoteAutority.Master)]
        public string text;

        [NetworkRPC(RemoteAutority.Master)]
        void Call(RpcInfo info)
        {
            transform.position += Vector3.one * 4;
        }

        void Start()
        {
            SyncVar("text", "Hello World");

            RPC(Call, RpcBufferMode.All);
        }
    }
}