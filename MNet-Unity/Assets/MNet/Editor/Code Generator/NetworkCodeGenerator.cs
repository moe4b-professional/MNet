#if UNITY_EDITOR
using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Assertions.Must;
using MB;
using UnityEngine.Experimental.AI;

namespace MNet
{
    public partial class NetworkCodeGenerator : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public const int DefaultCallbackOrder = 500;
        public int callbackOrder => DefaultCallbackOrder;

        public const string FilePath = "Assets/GeneratedNetworkCode.cs";

        public void OnPreprocessBuild(BuildReport report)
        {
            //Extract();
        }
        public void OnPostprocessBuild(BuildReport report)
        {
            //AssetDatabase.DeleteAsset(FilePath);
        }

        [MenuItem("MNet/Generate AOT Code")]
        static async void Extract()
        {
            var builder = new CodeBuilder();

            WriteStartTemplate(builder);
            {
                var rpcs = ExtractRPCs(builder);
                var syncvars = ExtractSyncVars();

                builder.AppendLine();
                await ExtractResolvers(builder, rpcs, syncvars);
            }
            WriteEndTemplate(builder);

            var text = builder.ToString();
            File.WriteAllText(FilePath, text);

            AssetDatabase.Refresh();
        }

        #region Template
        static void WriteStartTemplate(CodeBuilder builder)
        {
            const string Template =
                @"using UnityEngine;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;

[assembly : AssemblySymbolDefine(""MNet_Generated_AOT_Code"")]

[Preserve]
[CompilerGenerated]
public static class GeneratedNetworkCode
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	static void OnLoad()
	{
";

            builder.Append(Template);

            builder.Indentation += 2;
            builder.AppendLine();
        }

        static void WriteEndTemplate(CodeBuilder builder)
        {
            const string Template =
                @"
	}
}";

            builder.Append(Template);
        }
        #endregion

        #region RPCs
        static List<RpcBind.Prototype> ExtractRPCs(CodeBuilder builder)
        {
            var list = new List<RpcBind.Prototype>();

            builder.Append("//RPCs");
            builder.AppendLine();
            builder.Append('{');
            builder.Indentation += 1;
            builder.AppendLine();

            var types = TypeCache.GetTypesDerivedFrom<INetworkBehaviour>();

            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];

                var rpcs = RpcBind.Parser.Retrieve(type);
                if (rpcs.Length == 0) continue;

                list.AddRange(rpcs);

                builder.Append("//");
                GetTypeFullName(type, builder);
                builder.AppendLine();

                foreach (var rpc in rpcs)
                {
                    builder.Append("");

                    GetRPCName(rpc.Method, builder);

                    builder.Append(".Register();");

                    builder.AppendLine();
                }

                builder.AppendLine();
            }

            builder.Indentation -= 1;
            builder.AppendLine();
            builder.Append('}');
            builder.AppendLine();

            return list;
        }

        static void GetRPCName(MethodInfo info, CodeBuilder builder)
        {
            var parameters = info.GetParameters();

            builder.Append("MNet.");

            if (info.ReturnType == typeof(void))
                builder.Append("RpcBindVoid");
            else
                builder.Append("RpcBindReturn");

            if(info.ReturnType != typeof(void) || parameters.Length > 1)
            {
                builder.Append('<');

                if(info.ReturnType != typeof(void))
                {
                    GetTypeFullName(info.ReturnType, builder);
                }

                for (int i = 0; i < parameters.Length - 1; i++)
                {
                    GetTypeFullName(parameters[i].ParameterType, builder);

                    if (i < parameters.Length - 2)
                        builder.Append(", ");
                }

                builder.Append('>');
            }
        }
        #endregion

        #region Sync Vars
        static List<VariableInfo> ExtractSyncVars()
        {
            var list = new List<VariableInfo>();

            var types = TypeCache.GetTypesDerivedFrom<INetworkBehaviour>();

            foreach (var type in types)
            {
                var vars = SyncVar.Parser.Retrieve(type);
                list.AddRange(vars);
            }

            return list;
        }
        #endregion

        #region Resolvers
        static async Task ExtractResolvers(CodeBuilder builder, List<RpcBind.Prototype> rpcs, List<VariableInfo> syncvars)
        {
            var types = new HashSet<Type>();
            //Collect Types
            {
                var data = await Parser.Process();

                foreach (var usage in data.Usages)
                {
                    var type = Type.GetType(usage);

                    if (type.ContainsGenericParameters == true)
                    {
                        Debug.LogError(type);
                        continue;
                    }

                    types.Add(type);
                }

                foreach (var rpc in rpcs)
                {
                    foreach (var parameter in rpc.Method.GetParameters())
                        types.Add(parameter.ParameterType);

                    if (rpc.Method.ReturnType != typeof(void))
                        types.Add(rpc.Method.ReturnType);
                }

                foreach (var syncvar in syncvars)
                {
                    var argument = syncvar.ValueType.GenericTypeArguments[0];
                    types.Add(argument);
                }
            }

            var resolvers = new HashSet<Type>();
            //Creating Resolvers
            {
                foreach (var type in types)
                    ProcessResolver(type, ref resolvers);

                static void ProcessResolver(Type type, ref HashSet<Type> resolvers)
                {
                    if (DynamicNetworkSerialization.Resolve(type, out var resolver) == false)
                        return;

                    resolvers.Add(resolver.GetType());

                    var children = resolver.Children;
                    if (children == null) return;

                    foreach (var child in children)
                        ProcessResolver(child, ref resolvers);
                }
            }

            //Write Code
            {
                builder.Append("//Resolvers");
                builder.AppendLine();
                builder.Append('{');
                builder.Indentation += 1;
                builder.AppendLine();

                foreach (var resolver in resolvers)
                {
                    builder.Append("new ");
                    GetTypeFullName(resolver, builder);
                    builder.Append("();");

                    builder.AppendLine();
                }

                builder.Indentation -= 1;
                builder.AppendLine();
                builder.Append('}');
                builder.AppendLine();
            }
        }
        #endregion

        static void GetTypeFullName(Type type, CodeBuilder builder)
        {
            var name = type.ToString();

            for (int i = 0; i < name.Length; i++)
            {
                var character = name[i];

                if (character == '`')
                    break;
                else if (character == '+')
                    character = '.';

                builder.Append(character);
            }

            if (type.IsGenericType)
            {
                builder.Append('<');

                var arguments = type.GenericTypeArguments;

                for (int i = 0; i < arguments.Length; i++)
                {
                    GetTypeFullName(arguments[i], builder);

                    if (i < arguments.Length - 1)
                        builder.Append(", ");
                }

                builder.Append('>');
            }
        }

        public class CodeBuilder
        {
            StringBuilder builder;

            public int Indentation = 0;

            public override string ToString() => builder.ToString();

            public void AppendLine()
            {
                builder.AppendLine();
                builder.Append('\t', Indentation);
            }

            public void Append(string text) => builder.Append(text);
            public void Append(char character) => builder.Append(character);

            public CodeBuilder()
            {
                builder = new StringBuilder();
            }
        }
    }
}
#endif