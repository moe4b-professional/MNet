using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Pipes;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace MNet.Parser
{
    public class LocalizationParser
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Narrative System Parser";

            if (args.Length == 0)
            {
                args = new string[2];
                args[0] = @"C:\Projects\Monolith\MNet\MNet-Unity\MNet-Unity.sln";
                args[1] = "MNet Parser";
            }

            if (args.Length < 2)
            {
                Log("Insufficent Arguments, Need 2 Arguments");
                await Task.Delay(2000);
                return;
            }

            var filePath = args[0];
            var pipeName = args[1];

            SelectVisualStudioInstance();

            using (var workspace = MSBuildWorkspace.Create())
            {
                workspace.WorkspaceFailed += (sender, ev) => Console.WriteLine(ev.Diagnostic.Message);

                Console.WriteLine($"Loading solution '{filePath}'");
                var solution = await workspace.OpenSolutionAsync(filePath, new ProcessReporter());
                Console.WriteLine($"Finished loading solution '{filePath}'");

                var data = new Data();

                foreach (var project in solution.Projects)
                    await Populate(data, project);

                await Send(data, pipeName);
            }

            Log("Completed");
        }

        static async Task Populate(Data data, Project project)
        {
            var compilation = await project.GetCompilationAsync();

            var invocationWalker = new InvocationSyntaxWalker();
            var stringBuilder = new StringBuilder();

            foreach (var document in project.Documents)
            {
                var tree = await document.GetSyntaxTreeAsync();
                if (tree == null) continue;

                var root = await document.GetSyntaxRootAsync();
                if (root == null) continue;

                var semantic = compilation.GetSemanticModel(tree);

                invocationWalker.Visit(root);

                var invocations = invocationWalker.List;

                foreach (var invocation in invocations)
                {
                    var method = semantic.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    if (method == null) continue;

                    for (int i = 0; i < method.TypeParameters.Length; i++)
                    {
                        var parameter = method.TypeParameters[i];

                        if (Utility.MNet.NetworkSerializationGenerator.IsDefined(parameter))
                        {
                            var argument = method.TypeArguments[i];

                            if (argument.Kind != SymbolKind.NamedType && argument.Kind != SymbolKind.ArrayType)
                                continue;

                            argument.FullName(stringBuilder);
                            var id = stringBuilder.ToString();
                            data.Usages.Add(id);

                            stringBuilder.Clear();
                        }
                    }
                }

                invocationWalker.Clear();
            }
        }

        static async Task<bool> Send(Data data, string pipeName)
        {
            var json = JsonConvert.SerializeObject(data);

            var client = new NamedPipeClientStream("localhost", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            await client.ConnectAsync(10 * 1000);

            if (client.IsConnected == false)
            {
                client.Close();
                return false;
            }

            Log("Connected to Server");

            var raw = Encoding.UTF8.GetBytes(json);

            await client.WriteAsync(BitConverter.GetBytes(raw.Length), 0, sizeof(int));
            await client.WriteAsync(raw, 0, raw.Length);

            Log("Data Sent");

            await client.ReadAsync(new byte[1], 0, 1);

            client.Close();

            return true;
        }

        [JsonObject]
        public class Data
        {
            [JsonProperty]
            public HashSet<string> Usages { get; }

            public Data()
            {
                Usages = new HashSet<string>();
            }
        }

        static VisualStudioInstance SelectVisualStudioInstance()
        {
            var collection = MSBuildLocator.QueryVisualStudioInstances().ToArray();

            if (collection.Length == 0)
                throw new InvalidOperationException($"MS Build Locator Couldn't Find any Visual Studio Instances");

            var instance = collection.First();

            Log($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");
            MSBuildLocator.RegisterInstance(instance);

            return instance;
        }

        class ProcessReporter : IProgress<ProjectLoadProgress>
        {
            public void Report(ProjectLoadProgress progress)
            {
                var file = Path.GetFileName(progress.FilePath);

                if (progress.TargetFramework != null)
                    file += $" ({progress.TargetFramework})";

                Console.WriteLine($"{progress.Operation,-15} {progress.ElapsedTime,-15:m\\:ss\\.fffffff} {file}");
            }
        }

        static void Log() => Console.WriteLine();
        static void Log(object target) => Console.WriteLine(target);
    }

    public class InvocationSyntaxWalker : CSharpSyntaxWalker
    {
        public List<InvocationExpressionSyntax> List { get; }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);

            List.Add(node);
        }

        public void Clear()
        {
            List.Clear();
        }

        public InvocationSyntaxWalker()
        {
            List = new List<InvocationExpressionSyntax>();
        }
    }
}

public static class INamedTypeSymbolExtensions
{
    static readonly SymbolDisplayFormat Simple = new SymbolDisplayFormat
        (SymbolDisplayGlobalNamespaceStyle.Omitted,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable);

    public static string FullName(this ITypeSymbol type)
    {
        var builder = new StringBuilder();
        FullName(type, builder);
        return builder.ToString();
    }
    public static void FullName(this ITypeSymbol type, StringBuilder builder)
    {
        ISymbol previous = default;

        foreach (var part in type.ToDisplayParts(Simple))
        {
            switch (part.Kind)
            {
                case SymbolDisplayPartKind.ClassName:
                case SymbolDisplayPartKind.InterfaceName:
                case SymbolDisplayPartKind.StructName:
                case SymbolDisplayPartKind.NamespaceName:
                case SymbolDisplayPartKind.EnumName:
                    builder.Append(part.Symbol.MetadataName);
                    break;

                case SymbolDisplayPartKind.Punctuation:
                {
                    var text = part.ToString();

                    if (text == ".")
                    {
                        if (previous == null || previous.Kind == SymbolKind.Namespace)
                            builder.Append(".");
                        else
                            builder.Append("+");
                    }
                    else if (text == "[")
                    {
                        goto EarlyExit;
                    }
                    else
                    {
                        builder.Append(text);
                    }
                }
                break;

                default:
                    throw new ArgumentOutOfRangeException($"{part.Kind} | {part}");
            }

            if (part.Symbol != null)
                previous = part.Symbol;
        }

        EarlyExit:

        var array = type as IArrayTypeSymbol;
        var named = (array == null ? type : array.ElementType) as INamedTypeSymbol;

        if(array == null)
        {
            if(named.ConstructedFrom != type)
            {
                builder.Append("[");

                for (var i = 0; i < named.TypeArguments.Length; i++)
                {
                    var argument = named.TypeArguments[i];

                    if (i > 0)
                        builder.Append(",");

                    builder.Append("[");

                    if (argument is INamedTypeSymbol argType)
                        FullName(argType, builder);

                    builder.Append("]");
                }

                builder.Append("]");
            }
        }
        else
        {
            builder.Append('[');
            builder.Append(',', array.Rank - 1);
            builder.Append(']');
        }

        builder.Append(", ");
        builder.Append(named.ContainingAssembly.Name);
    }
}