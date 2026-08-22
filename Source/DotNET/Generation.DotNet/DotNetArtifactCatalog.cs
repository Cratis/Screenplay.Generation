// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Builds stable source-level identities for .NET symbols.
/// </summary>
public static class DotNetSubjectIds
{
    /// <summary>
    /// Gets the stable subject identity for a named type within a project identity.
    /// </summary>
    /// <param name="type">The type to identify.</param>
    /// <param name="projectIdentity">The stable project identity within the analyzed workspace.</param>
    /// <returns>The generation subject identity.</returns>
    public static SubjectId ForType(INamedTypeSymbol type, string projectIdentity) => new()
    {
        Value = $"dotnet://{Uri.EscapeDataString(projectIdentity)}/{type.ContainingAssembly.Identity.Name}/{MetadataName(type)}"
    };

    /// <summary>
    /// Gets the fully qualified metadata name of a type, including generic arity and nested-type separators.
    /// </summary>
    /// <param name="type">The type to name.</param>
    /// <returns>The fully qualified metadata name.</returns>
    public static string MetadataName(INamedTypeSymbol type)
    {
        var typeNames = new Stack<string>();
        for (var current = type; current is not null; current = current.ContainingType)
        {
            typeNames.Push(current.MetadataName);
        }

        var nestedName = string.Join('+', typeNames);
        var namespaceName = type.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : type.ContainingNamespace.ToDisplayString();

        return string.IsNullOrEmpty(namespaceName) ? nestedName : $"{namespaceName}.{nestedName}";
    }
}

/// <summary>
/// Catalogs every source-declared type in a Roslyn compilation.
/// </summary>
/// <param name="compilation">The compilation to catalog.</param>
public sealed class DotNetArtifactCatalog(Compilation compilation)
{
    /// <summary>
    /// Gets every source-declared type, including nested types, in canonical metadata-name order.
    /// </summary>
    public IReadOnlyList<INamedTypeSymbol> Types { get; } =
    [
        .. DotNetTypeDiscovery.TypesIn(compilation.Assembly.GlobalNamespace)
            .Where(_ => _.Locations.Any(location => location.IsInSource))
            .OrderBy(DotNetSubjectIds.MetadataName, StringComparer.Ordinal)
    ];

    /// <summary>
    /// Gets every type with at least one conventionally authored declaration in canonical metadata-name order.
    /// </summary>
    /// <remarks>
    /// This view uses generated path and header conventions. Use the authoritative-tree overloads on
    /// <see cref="DotNetSource"/> when generated source must not establish facts.
    /// </remarks>
    public IReadOnlyList<INamedTypeSymbol> AuthoredTypes =>
    [
        .. Types.Where(DotNetSource.HasAuthoredDeclaration)
    ];
}

static class DotNetTypeDiscovery
{
    public static IEnumerable<INamedTypeSymbol> TypesIn(INamespaceSymbol @namespace)
    {
        foreach (var childNamespace in @namespace.GetNamespaceMembers().OrderBy(_ => _.Name, StringComparer.Ordinal))
        {
            foreach (var type in TypesIn(childNamespace))
            {
                yield return type;
            }
        }

        foreach (var type in @namespace.GetTypeMembers().OrderBy(_ => _.MetadataName, StringComparer.Ordinal))
        {
            foreach (var candidate in TypeAndNestedTypes(type))
            {
                yield return candidate;
            }
        }
    }

    static IEnumerable<INamedTypeSymbol> TypeAndNestedTypes(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nested in type.GetTypeMembers().OrderBy(_ => _.MetadataName, StringComparer.Ordinal))
        {
            foreach (var candidate in TypeAndNestedTypes(nested))
            {
                yield return candidate;
            }
        }
    }
}
