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
    [AddComponentMenu(NetworkAPI.Path + "Tests/" + nameof(SyncVarTest))]
	public class SyncVarTest : NetworkBehaviour
    {
        public string field;
        [SyncVar(RemoteAutority.Owner | RemoteAutority.Master)]
        public string Field
        {
            get => field;
            set
            {
                Debug.Log($"Set {nameof(Field)}, Old: '{field}', New: '{value}'");

                field = value;
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if (Entity.IsMine) SetSyncVar(nameof(Field), Field, Application.platform.ToString());
        }
    }
}