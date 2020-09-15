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
    public class RpcTest : NetworkBehaviour
    {
        protected override void OnSpawn()
        {
            base.OnSpawn();

            RequestRPC(Rpc0);
            RequestRPC(Rpc1, 1);
            RequestRPC(Rpc2, 1, 2);
            RequestRPC(Rpc3, 1, 2, 3);
            RequestRPC(Rpc4, 1, 2, 3, 4);
            RequestRPC(Rpc5, 1, 2, 3, 4, 5);
            RequestRPC(Rpc6, 1, 2, 3, 4, 5, 6);
        }

        [NetworkRPC]
        void Rpc0(RpcInfo info)
        {
            Log();
        }

        [NetworkRPC]
        void Rpc1(int a, RpcInfo info)
        {
            Log(a);
        }

        [NetworkRPC]
        void Rpc2(int a, int b, RpcInfo info)
        {
            Log(a, b);
        }

        [NetworkRPC]
        void Rpc3(int a, int b, int c, RpcInfo info)
        {
            Log(a, b, c);
        }

        [NetworkRPC]
        void Rpc4(int a, int b, int c, int d, RpcInfo info)
        {
            Log(a, b, c, d);
        }

        [NetworkRPC]
        void Rpc5(int a, int b, int c, int d, int e, RpcInfo info)
        {
            Log(a, b, c, d, e);
        }

        [NetworkRPC]
        void Rpc6(int a, int b, int c, int d, int e, int f, RpcInfo info)
        {
            Log(a, b, c, d, e, f);
        }

        void Log(params object[] parameters)
        {
            var text = "RPC";

            for (int i = 0; i < parameters.Length; i++)
                text += $" {parameters[i]}";

            Debug.Log(text);
        }
    }
}