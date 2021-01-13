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
        public bool uniTask;

        public bool success = false;

        public const int Count = 7;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            for (int i = 0; i < Count; i++) results.Add(false);

            coroutine = false;
            uniTask = false;
            success = false;

            TargetRPC(Call0, NetworkAPI.Client.Self);
            TargetRPC(Call1, NetworkAPI.Client.Self, 0);
            TargetRPC(Call2, NetworkAPI.Client.Self, 0, 1);
            TargetRPC(Call3, NetworkAPI.Client.Self, 0, 1, 2);
            TargetRPC(Call4, NetworkAPI.Client.Self, 0, 1, 2, 3);
            TargetRPC(Call5, NetworkAPI.Client.Self, 0, 1, 2, 3, 4);
            TargetRPC(Call6, NetworkAPI.Client.Self, 0, 1, 2, 3, 4, 5);

            TargetRPC(CoroutineRPC, NetworkAPI.Client.Self);
            TargetRPC(UniTaskRPC, NetworkAPI.Client.Self);
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

            UpdateState();
        }

        [NetworkRPC]
        IEnumerator CoroutineRPC(RpcInfo info)
        {
            yield return new WaitForSeconds(2f);

            coroutine = true;

            UpdateState();
        }

        [NetworkRPC]
        async UniTask UniTaskRPC(RpcInfo info)
        {
            await UniTask.Delay(2000, cancellationToken: ASyncDespawnCancellation.Token);

            uniTask = true;

            UpdateState();
        }

        void UpdateState()
        {
            success = results.All(x => x) && coroutine && uniTask;
        }
    }
}