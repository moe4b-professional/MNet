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
using UnityEditor.SceneManagement;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using UnityEditor.Build.Reporting;

namespace MNet
{
    public static class MNetProjectUtility
    {
        public const string Path = Constants.Path;

        public const int Priority = 1000;

        public static class Launch
        {
            public const string Path = MNetProjectUtility.Path + "Launch/";

            public const int Priority = MNetProjectUtility.Priority + 1;

            public static class Server
            {
                public const string Path = Launch.Path + "Server/";

                public const string Folder = "MNet-Release/Servers/";

                public static string ParsePath(string name) => $"../{Folder}/{name}/Start (Windows).bat";

                [MenuItem(Path + "All", priority = Priority)]
                public static void All()
                {
                    Master();
                    Game();
                }

                [MenuItem(Path + "Master", priority = Priority)]
                public static void Master()
                {
                    var path = ParsePath("Master-Server");

                    StartProcess(path);
                }

                [MenuItem(Path + "Game", priority = Priority)]
                public static void Game()
                {
                    var path = ParsePath("Game-Server");

                    StartProcess(path);
                }
            }

            public static class Client
            {
                public const string Path = Launch.Path + "Client/";

                public const string Folder = "Build";
                public const string Release = "Windows";
                public static string File => Application.productName;

                [MenuItem(Path + "Mono", priority = Priority)]
                public static void Mono()
                {
                    var path = $"./{Folder}/{Release}/Mono/{File}.exe";

                    StartProcess(path);
                }

                [MenuItem(Path + "IL2CPP", priority = Priority)]
                public static void IL2CPP()
                {
                    var path = $"./{Folder}/{Release}/IL2CPP/{File}.exe";

                    StartProcess(path);
                }
            }
        }

        public static class Build
        {
            public const string Path = MNetProjectUtility.Path + "Build/";

            public const int Priority = MNetProjectUtility.Priority + 20;

            public const BuildOptions Options = BuildOptions.AutoRunPlayer /*| BuildOptions.ShowBuiltPlayer*/ | BuildOptions.Development | BuildOptions.StrictMode;

            [MenuItem(Path + "Mono", priority = Priority)]
            public static void Mono()
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);

                PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.Disabled);

                Perform($"Build/Windows/Mono/{Application.productName}.exe", BuildTarget.StandaloneWindows, Options);
            }

            [MenuItem(Path + "IL2CPP", priority = Priority)]
            public static void IL2CPP()
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);

                PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.High);

                Perform($"Build/Windows/IL2CPP/{Application.productName}.exe", BuildTarget.StandaloneWindows, Options);
            }

            public static BuildReport Perform(string path, BuildTarget target, BuildOptions options)
            {
                var scenes = EditorBuildSettings.scenes;

                var report = BuildPipeline.BuildPlayer(scenes, path, target, options);

                return report;
            }
        }

        public static class Package
        {
            public const int Priority = MNetProjectUtility.Priority + 300;

            public const string Destination = "../MNet-Release/";

            [MenuItem(Path + "Package", priority = Priority)]
            static void Perform()
            {
                var options = ExportPackageOptions.Interactive | ExportPackageOptions.Recurse;

                AssetDatabase.ExportPackage("Assets/MNet", Destination + "MNet-Unity.unitypackage", options);

                WriteVersion();
            }

            static void WriteVersion()
            {
                File.WriteAllText(Destination + "Version.txt", $"v{Constants.ApiVersion}");
            }
        }

        public static class QuickAccess
        {
            public const string Path = "Quick Access/";

            [MenuItem(Path + "Launch All Servers")]
            static void LaunchAllServers() => Launch.Server.All();

            [MenuItem(Path + "Launch Master Server")]
            static void LaunchMasterServer() => Launch.Server.Master();

            [MenuItem(Path + "Build Mono Client")]
            static void BuildMonoClient() => Build.Mono();

            [MenuItem(Path + "Launch Mono Client")]
            static void LaunchMonoClient() => Launch.Client.Mono();
        }

        public static class Scenes
        {
            public const string Path = "Scenes/";

            [MenuItem(Path + "Main Menu")]
            static void MainMenu() => Load("Main Menu");

            [MenuItem(Path + "Level 1")]
            static void Level1() => Load("Level 1");

            static void Load(string name)
            {
                var scene = EditorBuildSettings.scenes.First(x => x.path.EndsWith(name + ".unity"));

                EditorSceneManager.OpenScene(scene.path);
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