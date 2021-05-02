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
    [AddComponentMenu(Constants.Path + "Tests/" + "Sync Var Test")]
    public class SyncVarTest : NetworkBehaviour
    {
        [SyncVar(Authority = RemoteAuthority.Owner)]
        [SerializeField]
        string field = default;

        void FieldSyncHook(string oldValue, string newValue, SyncVarInfo info)
        {
            if (success == false) success = true;
        }

        [SerializeField]
        bool success = false;

        public override void OnNetwork()
        {
            base.OnNetwork();

            Network.OnSpawn += OnSpawn;
        }

        void OnSpawn()
        {
            Network.RegisterSyncVarHook(nameof(field), field, FieldSyncHook);

            if (Entity.IsMine == false) return;

            Network.BroadcastSyncVar(nameof(field), field, NetworkAPI.Client.Profile.Name);
        }
    }
}