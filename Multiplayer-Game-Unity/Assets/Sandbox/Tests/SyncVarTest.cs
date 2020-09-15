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

using Backend;

namespace Game
{
	public class SyncVarTest : NetworkBehaviour
	{
        public string field;
        [SyncVar(EntityAuthorityType.Owner)]
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

            if (Entity.IsMine == false) return;

            RequestSyncVar(nameof(Field), Application.platform.ToString());
        }
    }
}