// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet;

static class DotNetHostCapabilities
{
    public static ImmutableArray<AdapterHostCapability> Determine(DotNetAnalysisContext context)
    {
        if (context.Projects.Count == 0)
        {
            return [];
        }

        var capabilities = ImmutableArray.CreateBuilder<AdapterHostCapability>();
        if (context.Projects.All(HasAuthoritativeAuthoredSource))
        {
            capabilities.Add(AdapterHostCapability.AuthoredSource);
        }

        if (context.Projects.All(project =>
                project.Compilation is not null &&
                string.Equals(project.Compilation.Language, LanguageNames.CSharp, StringComparison.Ordinal)))
        {
            capabilities.Add(AdapterHostCapability.SemanticAnalysis);
        }

        if (context.Projects.All(HasStableSourceIdentity))
        {
            capabilities.Add(AdapterHostCapability.StableSourceLocations);
        }

        if (HasAvailableProjectReferences(context.Projects))
        {
            capabilities.Add(AdapterHostCapability.ProjectReferences);
        }

        return capabilities.ToImmutable();
    }

    static bool HasAuthoritativeAuthoredSource(DotNetProjectCompilation project)
    {
        if (project.Compilation is null || project.AuthoredSyntaxTrees is null)
        {
            return false;
        }

        var compilationTrees = project.Compilation.SyntaxTrees.ToHashSet();
        return project.AuthoredSyntaxTrees.All(tree =>
            compilationTrees.Contains(tree) &&
            !DotNetGeneratedSource.IsGenerated(tree));
    }

    static bool HasStableSourceIdentity(DotNetProjectCompilation project)
    {
        if (!HasAuthoritativeAuthoredSource(project) || project.SourceContext is null)
        {
            return false;
        }

        return project.AuthoredSyntaxTrees.All(tree =>
            project.SourceContext.Files.TryGetValue(tree, out var file) &&
            file.Identity.Project == project.SourceContext.ProjectIdentity &&
            !string.IsNullOrWhiteSpace(file.Identity.Path) &&
            !string.IsNullOrWhiteSpace(file.DisplayPath));
    }

    static bool HasAvailableProjectReferences(IReadOnlyList<DotNetProjectCompilation> projects)
    {
        var assemblies = projects
            .Select(project => project.Compilation.Assembly.Identity)
            .ToHashSet();
        return projects.Any(project => project.Compilation.ReferencedAssemblyNames.Any(assemblies.Contains));
    }
}
