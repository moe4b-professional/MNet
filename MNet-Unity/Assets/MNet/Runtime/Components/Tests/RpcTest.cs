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

using Cysharp.Threading.Tasks;

namespace MNet
{
    [AddComponentMenu(Constants.Path + "Tests/" + "RPC Test")]
    public class RpcTest : NetworkBehaviour
    {
        public List<bool> results = new List<bool>();

        public bool coroutine;
        public bool async;

        public bool success = false;

        public const int Count = 7;

        public override void OnNetwork()
        {
            base.OnNetwork();

            Network.OnSpawn += SpawnCallback;
        }

        void SpawnCallback()
        {
            for (int i = 0; i < Count; i++) results.Add(false);

            success = false;

            Network.TargetRPC(Call0, NetworkAPI.Client.Self).Send();
            Network.TargetRPC(Call1, NetworkAPI.Client.Self, 0).Send();
            Network.TargetRPC(Call2, NetworkAPI.Client.Self, 0, 1).Send();
            Network.TargetRPC(Call3, NetworkAPI.Client.Self, 0, 1, 2).Send();
            Network.TargetRPC(Call4, NetworkAPI.Client.Self, 0, 1, 2, 3).Send();
            Network.TargetRPC(Call5, NetworkAPI.Client.Self, 0, 1, 2, 3, 4).Send();
            Network.TargetRPC(Call6, NetworkAPI.Client.Self, 0, 1, 2, 3, 4, 5).Send();

            Network.TargetRPC(CallCoroutine, NetworkAPI.Client.Self).Send();
            Network.TargetRPC(CallAsync, NetworkAPI.Client.Self).Send();
        }

        [NetworkRPC] void Call0(RpcInfo info) => Call();
        [NetworkRPC] void Call1(int a, RpcInfo info) => Call(a);
        [NetworkRPC] void Call2(int a, int b, RpcInfo info) => Call(a, b);
        [NetworkRPC] void Call3(int a, int b, int c, RpcInfo info) => Call(a, b, c);
        [NetworkRPC] void Call4(int a, int b, int c, int d, RpcInfo info) => Call(a, b, c, d);
        [NetworkRPC] void Call5(int a, int b, int c, int d, int e, RpcInfo info) => Call(a, b, c, d, e);
        [NetworkRPC] void Call6(int a, int b, int c, int d, int e, int f, RpcInfo info) => Call(a, b, c, d, e, f);
        [NetworkRPC] IEnumerator CallCoroutine(RpcInfo info)
        {
            yield return new WaitForSeconds(0.5f);

            coroutine = true;

            UpdateState();
        }
        [NetworkRPC] async UniTask CallAsync(RpcInfo info)
        {
            await UniTask.Delay(500);

            async = true;

            UpdateState();
        }

        void Call(params int[] arguments)
        {
            var input = arguments.Sum();

            var expectation = Enumerable.Range(0, arguments.Length).Sum();

            if (input == expectation) results[arguments.Length] = true;

            UpdateState();
        }

        void UpdateState()
        {
            success = results.All(x => x) && coroutine && async;
        }
    }
}