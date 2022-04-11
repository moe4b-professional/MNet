using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Utility
{
    public static class MNet
    {
        public readonly static string Namespace = "MNet";

        public static class NetworkSerializationGenerator
        {
            public readonly static string Title = nameof(NetworkSerializationGenerator);
            public readonly static string ID = $"{Namespace}.{Title}Attribute";

            public static bool IsDefined(ISymbol symbol)
            {
                var attributes = symbol.GetAttributes();

                return IsAttributeDefined(attributes, ID);
            }
        }
    }

    public static bool IsAttributeDefined(ImmutableArray<AttributeData> attributes, string id)
    {
        for (int i = 0; i < attributes.Length; i++)
            if (attributes[i].AttributeClass.ToString() == id)
                return true;

        return false;
    }

    public static IEnumerable<ParameterInvocationEntry> IterateTypeParameters(InvocationExpressionSyntax invocation, IMethodSymbol method)
    {
        if (method == null) yield break;

        var arguments = invocation.ArgumentList.Arguments;

        for (int i = 0; i < method.TypeParameters.Length; i++)
        {
            if (i >= arguments.Count) break;

            yield return new ParameterInvocationEntry(arguments[i], method.Parameters[i]);
        }
    }
}

public class ParameterInvocationEntry
{
    public ArgumentSyntax Invocation { get; }
    public IParameterSymbol Symbol { get; }

    public ParameterInvocationEntry(ArgumentSyntax invocation, IParameterSymbol symbol)
    {
        this.Invocation = invocation;
        this.Symbol = symbol;
    }
}