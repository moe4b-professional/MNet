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
        public bool success = false;

        public override void OnNetwork()
        {
            base.OnNetwork();

            Network.OnSpawn += SpawnCallback;
        }

        async void SpawnCallback()
        {
            var name = NetworkAPI.Room.Master.Client.Name;

            var answer = await Network.QueryRPC(QueryRPC, NetworkAPI.Room.Master.Client).Send();

            if (answer.Success == false)
                Debug.LogError($"RPR Test Failed, Response: {answer.Response}");

            success = answer.Value == name;
        }

        [NetworkRPC]
        FixedString32 QueryRPC(RpcInfo info)
        {
            return NetworkAPI.Client.Profile.Name;
        }
    }
}