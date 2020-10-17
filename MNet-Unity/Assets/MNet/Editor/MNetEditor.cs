﻿#if UNITY_EDITOR
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
    public static class MNetEditor
    {
        [MenuItem(MNetAPI.Path + "Configuration", false, 0)]
        static void Configuration()
        {
            var asset = MNetAPIConfig.Load();

            Selection.activeObject = asset;
        }
    }
}
#endif