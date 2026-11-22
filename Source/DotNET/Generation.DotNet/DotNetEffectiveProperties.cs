// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet;

static class DotNetEffectiveProperties
{
    public static IReadOnlyList<IPropertySymbol> Of(INamedTypeSymbol type, DotNetAnalysisContext context, DotNetMemberSurfaceScope scope)
    {
        if (scope == DotNetMemberSurfaceScope.Declared)
        {
            return DotNetTypeShapes.PublicReadablePropertiesOf(type);
        }

        if (scope != DotNetMemberSurfaceScope.Inherited)
        {
            throw new ArgumentOutOfRangeException(nameof(scope), scope, "The member surface scope is not defined.");
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        var properties = new List<IPropertySymbol>();
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            properties.AddRange(DotNetTypeShapes.PublicReadablePropertiesOf(current).Where(property => IsAuthored(property, context) && names.Add(property.Name)));
        }

        return properties;
    }

    static bool IsAuthored(IPropertySymbol property, DotNetAnalysisContext context)
    {
        var original = property.OriginalDefinition;
        if (HasAuthoredDeclaration(original, context))
        {
            return true;
        }

        var metadataName = DotNetSubjectIds.MetadataName(original.ContainingType);
        var declarations = context.Projects
            .Where(project => project.Compilation.Assembly.Identity.Equals(original.ContainingAssembly.Identity))
            .Select(project => project.Compilation.GetTypeByMetadataName(metadataName))
            .Where(type => type is not null)
            .SelectMany(type => type!.GetMembers(original.Name).OfType<IPropertySymbol>())
            .Where(candidate => !candidate.IsIndexer)
            .ToArray();
        return declarations.Length == 1 && HasAuthoredDeclaration(declarations[0], context);
    }

    static bool HasAuthoredDeclaration(IPropertySymbol property, DotNetAnalysisContext context) =>
        property.OriginalDefinition.DeclaringSyntaxReferences.Any(reference =>
            !DotNetGeneratedSource.IsGenerated(reference.SyntaxTree) &&
            context.Projects.Any(project =>
                project.AuthoredSyntaxTrees.Contains(reference.SyntaxTree) &&
                project.Compilation.SyntaxTrees.Contains(reference.SyntaxTree)));
}
