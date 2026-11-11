// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.given;

/// <summary>
/// Renders the bounded extraction result of every field initializer of a source as ordered text, so
/// characterization specs can pin kinds, messages and exact coordinates as literals.
/// </summary>
public class a_value_snapshot : a_compilation
{
    protected static string[] SnapshotOf(string path, string source)
    {
        var compilation = CompilationFrom(new SourceFile(path, source));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        return
        [
            .. tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>()
                .Where(declarator => declarator.Parent?.Parent is FieldDeclarationSyntax && declarator.Initializer is not null)
                .Select(declarator => $"{declarator.Identifier.ValueText}: {Describe(DotNetSourceValues.Extract(declarator.Initializer.Value, semanticModel))}")
        ];
    }

    static string Describe(DotNetBounded<DotNetSourceValue> result) => result switch
    {
        DotNetKnown<DotNetSourceValue> known => $"known {Describe(known.Value)}",
        DotNetUnknown<DotNetSourceValue> unknown => $"unknown [{string.Join("; ", unknown.Failures.Select(Describe))}]",
        _ => "unexpected"
    };

    static string Describe(DotNetValueFailure failure) => $"{failure.Kind} {Span(failure.Source)} '{failure.Message}'";

    static string Describe(DotNetSourceValue value) => value switch
    {
        DotNetConstantValue { Value: ISymbol symbol } constant => $"field({symbol.ToDisplayString()}:{constant.Type?.ToDisplayString()})",
        DotNetConstantValue constant => $"const({Convert.ToString(constant.Value, CultureInfo.InvariantCulture) ?? "null"}:{constant.Type?.ToDisplayString()})",
        DotNetTypeValue typeValue => $"typeof({typeValue.Type.ToDisplayString()})",
        DotNetPayloadValue payload => $"payload({payload.Type.ToDisplayString()}){{{string.Join(", ", payload.Values.Select(_ => $"{_.Name}@{Span(_.Source)}={Describe(_.Value)}"))}}}",
        DotNetCollectionValue collection => $"collection({collection.Type?.ToDisplayString()})[{string.Join(", ", collection.Values.Select(_ => $"{Span(_.Source)}={Describe(_.Value)}"))}]",
        _ => "unexpected"
    };

    static string Span(Location location)
    {
        var span = location.GetLineSpan().Span;
        return $"{span.Start.Line}:{span.Start.Character}-{span.End.Line}:{span.End.Character}";
    }
}
