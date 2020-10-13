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

namespace MNet
{
    public static class MNetEditor
    {
        public static class Server
        {
            public const string Folder = "MNet-Core";
            public const string Release = "Debug";
            public const string Variant = "netcoreapp3.1";

            public static string ParsePath(string name) => $"../{Folder}/{name}/bin/{Release}/{Variant}/{name}.exe";

            [MenuItem("Launch/Servers")]
            public static void Launch()
            {
                LaunchMaster();

                LaunchGame();
            }

            public static void LaunchMaster()
            {
                var path = ParsePath("MasterServer");

                StartProcess(path);
            }

            public static void LaunchGame()
            {
                var path = ParsePath("GameServer");

                StartProcess(path);
            }
        }

        public static class Build
        {
            public const string Folder = "Build";
            public const string Release = "Windows";
            public static string File => Application.productName;

            [MenuItem("Launch/Build")]
            public static void LaunchBuild()
            {
                var path = $"./{Folder}/{Release}/{File}.exe";

                StartProcess(path);
            }
        }

        static System.Diagnostics.Process StartProcess(string path)
        {
            var file = new FileInfo(path);

            return StartProcess(file);
        }
        static System.Diagnostics.Process StartProcess(FileInfo file)
        {
            var directory = file.Directory;

            var process = new System.Diagnostics.ProcessStartInfo(file.FullName);
            process.WorkingDirectory = directory.FullName;

            return System.Diagnostics.Process.Start(process);
        }
    }
}
#endif