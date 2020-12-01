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
    [AddComponentMenu(Constants.Path + "Tests/" + "RPR Test")]
    public class RprTest : NetworkBehaviour
    {
        public const string Payload = "Hello World";

        public bool success = false;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            ReturnRPC(Call, NetworkAPI.Room.Master, Callback);
        }

        [NetworkRPC(Authority = RemoteAuthority.Any)]
        string Call(RpcInfo info) => Payload;

        void Callback(RprResult result, string value)
        {
            if(result != RprResult.Success)
            {
                Debug.LogError("RPR Test Failed: " + result);
                return;
            }

            success = value == Payload;
        }
    }
}