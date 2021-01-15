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
    [AddComponentMenu(Constants.Path + "Tests/" + "RPR Test")]
    public class RprTest : NetworkBehaviour
    {
        public string value;

        public bool success = false;

        protected async override void OnSpawn()
        {
            base.OnSpawn();

            var answer = await QueryAsyncRPC(AsyncQueryRPC, NetworkAPI.Room.Master.Client);

            success = answer.Success;

            if (answer.Success)
                value = answer.Value;
            else
                Debug.LogError($"RPR Test Failed, Response: {answer.Response}");
        }

        [NetworkRPC]
        async UniTask<string> AsyncQueryRPC(RpcInfo info)
        {
            await UniTask.Delay(4000, cancellationToken: ASyncDespawnCancellation.Token);

            return NetworkAPI.Client.Profile.Name;
        }
    }
}