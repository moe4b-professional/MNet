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
    public class RpcTestBehaviour : NetworkBehaviour
    {
        protected override void OnSpawn()
        {
            base.OnSpawn();

            RequestRPC(Rpc, NetworkAPI.Room.Master, Callback, "Hello");

            return;

            RequestRPC(nameof(Rpc0), RpcBufferMode.None);
            RequestRPC(nameof(Rpc1), RpcBufferMode.None, 1);
            RequestRPC(nameof(Rpc2), RpcBufferMode.None, 1, 2);
            RequestRPC(nameof(Rpc3), RpcBufferMode.None, 1, 2, 3);
            RequestRPC(nameof(Rpc4), RpcBufferMode.None, 1, 2, 3, 4);
            RequestRPC(nameof(Rpc5), RpcBufferMode.None, 1, 2, 3, 4, 5);
            RequestRPC(nameof(Rpc6), RpcBufferMode.None, 1, 2, 3, 4, 5, 6);
        }

        [NetworkRPC(RpcAuthority.Any)]
        string Rpc(string text, RpcInfo info)
        {
            return Application.platform.ToString();
        }

        void Callback(RprResult result, string value)
        {
            if (result == RprResult.Success)
            {
                Debug.Log("RPR: " + value);
            }
            else
            {
                Debug.LogError("RPR Failed: " + result);
            }
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