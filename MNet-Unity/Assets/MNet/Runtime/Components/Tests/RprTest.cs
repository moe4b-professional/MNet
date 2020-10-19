﻿using System;
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
    [AddComponentMenu(NetworkAPI.Path + "Tests/" + nameof(RprTest))]
    public class RprTest : NetworkBehaviour
    {
        protected override void OnSpawn()
        {
            base.OnSpawn();

            RPC(RPC, NetworkAPI.Room.Master, Callback, "Hello");
        }

        [NetworkRPC(RemoteAutority.Any)]
        string RPC(string text, RpcInfo info)
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
    }
}