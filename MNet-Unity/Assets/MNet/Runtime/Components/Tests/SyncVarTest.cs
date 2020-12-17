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
        [SerializeField]
        string field;
        [SyncVar(Authority = RemoteAuthority.Owner | RemoteAuthority.Master)]
        public string Field
        {
            get => field;
            private set
            {
                //Do your things here, new value: value, old value: field
                field = value;
            }
        }

        void SyncField(string value) => SyncVar(nameof(Field), Field, value);

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if (Entity.IsMine == false) return;

            SyncField(NetworkAPI.Client.Profile.Name);
        }
    }
}