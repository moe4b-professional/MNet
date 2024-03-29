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

namespace MNet.Example
{
	public class ClientListUITemplate : UITemplate<ClientListUITemplate, NetworkClient>
	{
        [SerializeField]
        Text label = default;

        public override void SetData(NetworkClient data)
        {
            base.SetData(data);

            Rename(Data.Name.ToString());
        }

        public override void UpdateState()
        {
            base.UpdateState();

            label.text = $"{Data.Name}";
        }
    }
}