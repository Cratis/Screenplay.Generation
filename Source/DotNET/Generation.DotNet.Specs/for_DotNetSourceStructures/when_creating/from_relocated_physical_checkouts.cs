// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructures.when_creating;

public class from_relocated_physical_checkouts : given.a_compilation
{
    string _first = null!;
    string _second = null!;

    void Because()
    {
        _first = SnapshotAt("/first-checkout");
        _second = SnapshotAt("/second-checkout");
    }

    [Fact] void should_produce_the_same_canonical_structure() => _first.ShouldEqual(_second);
    [Fact] void should_not_expose_either_physical_checkout() => new[] { _first, _second }.Any(_ => _.Contains("checkout", StringComparison.Ordinal)).ShouldBeFalse();

    static string Canonical(DotNetSourceStructureSnapshot snapshot)
    {
        var structure = snapshot.Structures.Single();
        return $"{structure.Project}|{structure.ProjectRole}|{structure.Subject.Value}|{structure.Namespace}|" +
               $"{string.Join(',', structure.ProjectRelativePaths)}|{structure.Source!.Path}|{structure.Source.FileIdentity}";
    }

    string SnapshotAt(string root)
    {
        var compilation = CompilationFrom(new SourceFile(
            $"{root}/Banking/Source/Accounts/Register/Register.cs",
            "namespace Banking.Accounts.Register; public record Register;"));
        var tree = compilation.SyntaxTrees.Single();
        var sourceContext = DotNetSourcePaths.Create(
            "apps/Banking",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = tree,
                    ProjectRelativePath = "Source/Accounts/Register/Register.cs",
                    WorkspaceRelativePath = "apps/Banking/Source/Accounts/Register/Register.cs"
                }
            ]);
        var project = new DotNetProjectCompilation
        {
            Name = "Banking",
            Compilation = compilation,
            SourceContext = sourceContext,
            AuthoredSyntaxTrees = new HashSet<SyntaxTree> { tree }
        };

        return Canonical(DotNetSourceStructures.Create(new DotNetAnalysisContext([project])));
    }
}
