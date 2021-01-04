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

            QueryRPC(Call, NetworkAPI.Room.Master, Return);
        }

        [NetworkRPC(Authority = RemoteAuthority.Any)]
        string Call(RpcInfo info) => Payload;

        [NetworkRPC]
        void Return(RemoteResponseType response, string value)
        {
            if (response != RemoteResponseType.Success)
            {
                Debug.LogError("RPR Test Failed, Response: " + response);
                return;
            }

            success = value == Payload;
        }
    }
}