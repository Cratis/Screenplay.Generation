// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructures.when_creating;

public class from_projects_in_reversed_order : given.a_compilation
{
    string _forward = null!;
    string _reversed = null!;

    void Because()
    {
        var banking = Project("Banking", "Banking.Accounts.Register", "Register");
        var shipping = Project("Shipping", "Shipping.Parcels.Dispatch", "Dispatch");

        _forward = Canonical(DotNetSourceStructures.Create(new DotNetAnalysisContext([banking, shipping])));
        _reversed = Canonical(DotNetSourceStructures.Create(new DotNetAnalysisContext([shipping, banking])));
    }

    [Fact] void should_produce_the_same_canonical_snapshot() => _forward.ShouldEqual(_reversed);

    static string Canonical(DotNetSourceStructureSnapshot snapshot) => string.Join(
        '|',
        snapshot.Structures.Select(_ => $"{_.Project}:{_.Subject.Value}:{_.Namespace}:{string.Join(',', _.ProjectRelativePaths)}"));

    static DotNetProjectCompilation Project(string name, string @namespace, string typeName)
    {
        var path = $"Source/{typeName}/{typeName}.cs";
        var compilation = CompilationFrom(new SourceFile(
            $"/physical-checkout/{name}/{path}",
            $"namespace {@namespace}; public record {typeName};"));
        var tree = compilation.SyntaxTrees.Single();
        var sourceContext = DotNetSourcePaths.Create(
            $"apps/{name}",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = tree,
                    ProjectRelativePath = path,
                    WorkspaceRelativePath = $"apps/{name}/{path}"
                }
            ]);
        return new DotNetProjectCompilation
        {
            Name = name,
            Compilation = compilation,
            SourceContext = sourceContext,
            AuthoredSyntaxTrees = new HashSet<SyntaxTree> { tree }
        };
    }
}
