#if UNITY_EDITOR
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

using System.Diagnostics;

namespace Backend
{
    public static class Server
    {
        [MenuItem("Utility/Start Server")]
        public static void Start()
        {
            var folder = "Multiplayer-Game-Core";
            var name = "Server";
            var release = "Debug";
            var variant = "netcoreapp3.1";

            var path = $"../{folder}/{name}/bin/{release}/{variant}/{name}.exe";

            var file = new FileInfo(path);

            Process.Start(file.FullName);
        }
    }
}
#endif