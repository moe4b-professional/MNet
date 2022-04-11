using System;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
#endif

using Newtonsoft.Json;

using System.Threading.Tasks;
using System.IO.Pipes;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Text;
using System.Threading;

using UnityEngine.SceneManagement;
using System.Linq;
using System.Reflection;
using UnityEngine.Windows;

using MB;

namespace MNet
{
#if UNITY_EDITOR
    public partial class NetworkCodeGenerator
    {
        public static class Parser
        {
            public const string PipeName = "MNet Parser";

            static bool IsRunning = false;
            public static async Task<Structure> Process()
            {
                if (IsRunning) throw new InvalidOperationException("MNet Parsing Already in Progress");
                IsRunning = true;

                var pipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                Debug.Log("MNet Parser Pipe Server Started");

                try
                {
                    using var parser = Executable.Start();

                    var cancellation = new CancellationTokenSource();

                    parser.Exited += InvokeCancellation;
                    void InvokeCancellation(object sender, EventArgs args)
                    {
                        Debug.LogError($"MNet Parsing Cancelled, Process Halted Early");
                        cancellation.Cancel();
                    }

                    await pipe.WaitForConnectionAsync(cancellationToken: cancellation.Token);
                    Debug.Log("MNet Parser Pipe Server Connected");

                    var marker = new byte[sizeof(int)];
                    await pipe.ReadAsync(marker, cancellationToken: cancellation.Token);

                    var length = BitConverter.ToInt32(marker);

                    var raw = new byte[length];
                    await pipe.ReadAsync(raw, cancellationToken: cancellation.Token);
                    Debug.Log("MNet Parser Pipe Server Recieved Data");

                    parser.Exited -= InvokeCancellation;

                    await pipe.WriteAsync(new byte[1]);

                    var text = Encoding.UTF8.GetString(raw);

                    var structure = Structure.Parse(text);
                    return structure;
                }
                finally
                {
                    pipe.Close();
                    Debug.Log("MNet Parser Pipe Server Closed");
                    IsRunning = false;
                }
            }

            static string GetSolutionPath()
            {
                var file = System.IO.Path.GetFullPath($"{Application.productName}.sln");

                return file;
            }

            public static class Executable
            {
                public const string DirectoryRelativePath = "Editor/Code Generator/Parser/Executable/MNet-Parser.exe";

                public static Process Start()
                {
                    if (Application.platform != RuntimePlatform.WindowsEditor)
                        throw new InvalidOperationException($"Can Only Start Localization Parser from Windows Editor");

                    var target = RetrievePath();

                    var solution = GetSolutionPath();
                    var arguments = MUtility.FormatProcessArguments(solution, PipeName);

                    var info = new ProcessStartInfo(target, arguments);
                    info.CreateNoWindow = true;
                    info.UseShellExecute = false;
                    var process = System.Diagnostics.Process.Start(info);

                    process.EnableRaisingEvents = true;

                    return process;
                }

                public static string RetrievePath()
                {
                    var target = Assembly.GetExecutingAssembly().GetName().Name;

                    foreach (var assembly in AssetCollection.FindAll<AssemblyDefinitionAsset>())
                    {
                        if (assembly.name == target)
                        {
                            var path = AssetDatabase.GetAssetPath(assembly);

                            path = System.IO.Path.GetDirectoryName(path);
                            path = System.IO.Path.Combine(path, DirectoryRelativePath);
                            path = System.IO.Path.GetFullPath(path);

                            return path;
                        }
                    }

                    throw new Exception("Localization Parser Executable Couldn't be Found");
                }
            }

            [JsonObject]
            public class Structure
            {
                [JsonProperty]
                public HashSet<string> Usages { get; set; }

                public static Structure Parse(string json)
                {
                    if (string.IsNullOrEmpty(json))
                        return null;

                    var structure = JsonConvert.DeserializeObject<Structure>(json);
                    return structure;
                }
            }
        }
    }
#endif
}