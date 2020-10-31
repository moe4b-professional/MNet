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
    [AddComponentMenu(Constants.Path + "Tests/" + "RPC Test")]
    public class RpcTest : NetworkBehaviour
    {
        public List<bool> results = new List<bool>();

        public bool success = false;

        public const int Count = 7;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            for (int i = 0; i < Count; i++) results.Add(false);

            RPC(Call0);
            RPC(Call1, 0);
            RPC(Call2, 0, 1);
            RPC(Call3, 0, 1, 2);
            RPC(Call4, 0, 1, 2, 3);
            RPC(Call5, 0, 1, 2, 3, 4);
            RPC(Call6, 0, 1, 2, 3, 4, 5);
        }

        [NetworkRPC] void Call0(RpcInfo info) => Call();
        [NetworkRPC] void Call1(int a, RpcInfo info) => Call(a);
        [NetworkRPC] void Call2(int a, int b, RpcInfo info) => Call(a, b);
        [NetworkRPC] void Call3(int a, int b, int c, RpcInfo info) => Call(a, b, c);
        [NetworkRPC] void Call4(int a, int b, int c, int d, RpcInfo info) => Call(a, b, c, d);
        [NetworkRPC] void Call5(int a, int b, int c, int d, int e, RpcInfo info) => Call(a, b, c, d, e);
        [NetworkRPC] void Call6(int a, int b, int c, int d, int e, int f, RpcInfo info) => Call(a, b, c, d, e, f);

        void Call(params int[] arguments)
        {
            var input = arguments.Sum();

            var expectation = Enumerable.Range(0, arguments.Length).Sum();

            if (input == expectation) results[arguments.Length] = true;

            success = results.Aggregate((x, z) => x & z);
        }
    }
}