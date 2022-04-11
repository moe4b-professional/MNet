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

        public override void OnNetwork()
        {
            base.OnNetwork();

            Network.OnSpawn += SpawnCallback;
        }

        async void SpawnCallback()
        {
            {
                var answer = await Network.QueryRPC(QueryRPC, NetworkAPI.Room.Master.Client).Send();

                if (answer.Success)
                    value = answer.Value;
                else
                    Debug.LogError($"RPR Test Failed, Response: {answer.Response}");
            }

            success = value != string.Empty;
        }

        [NetworkRPC]
        string QueryRPC(RpcInfo info)
        {
            return NetworkAPI.Client.Profile.Name;
        }
    }
}