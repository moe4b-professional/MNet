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
    public static class Editor
    {
        static void StartProcess(FileInfo file)
        {
            var directory = file.Directory;

            var process = new ProcessStartInfo(file.FullName);
            process.WorkingDirectory = directory.FullName;

            Process.Start(process);
        }

        [MenuItem("Utility/Launch Game Server")]
        public static void LaunchGameServer()
        {
            var folder = "Multiplayer-Game-Core";
            var name = "GameServer";
            var release = "Debug";
            var variant = "netcoreapp3.1";

            var path = $"../{folder}/{name}/bin/{release}/{variant}/{name}.exe";

            var file = new FileInfo(path);

            StartProcess(file);
        }

        [MenuItem("Utility/Launch Master Server")]
        public static void LaunchMasterServer()
        {
            var folder = "Multiplayer-Game-Core";
            var name = "MasterServer";
            var release = "Debug";
            var variant = "netcoreapp3.1";

            var path = $"../{folder}/{name}/bin/{release}/{variant}/{name}.exe";

            var file = new FileInfo(path);

            StartProcess(file);
        }

        [MenuItem("Utility/Launch Build")]
        public static void LaunchBuild()
        {
            var folder = "Build";
            var release = "Windows";
            var name = "Multiplayer-Game-Unity";

            var path = $"./{folder}/{release}/{name}.exe";

            var file = new FileInfo(path);

            StartProcess(file);
        }
    }
}
#endif