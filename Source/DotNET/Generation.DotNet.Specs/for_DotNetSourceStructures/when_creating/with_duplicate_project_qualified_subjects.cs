// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructures.when_creating;

public class with_duplicate_project_qualified_subjects : given.a_compilation
{
    DotNetSourceStructureSnapshot _snapshot = null!;

    void Because() => _snapshot = DotNetSourceStructures.Create(new DotNetAnalysisContext(
    [
        Project("apps/first", "/first-checkout"),
        Project("apps/second", "/second-checkout")
    ]));

    [Fact] void should_fail_closed() => _snapshot.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_choose_one_project() => _snapshot.Structures.ShouldBeEmpty();
    [Fact] void should_report_the_duplicate_subject() => _snapshot.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.DuplicateSourceSubject);
    [Fact] void should_report_a_conflict_outcome() => _snapshot.Diagnostics.Single().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Conflict);

    static DotNetProjectCompilation Project(string projectIdentity, string checkout)
    {
        var compilation = CompilationFrom(new SourceFile(
            $"{checkout}/Banking/Source/Accounts/Register/Register.cs",
            "namespace Banking.Accounts.Register; public record Register;"));
        var tree = compilation.SyntaxTrees.Single();
        var sourceContext = DotNetSourcePaths.Create(
            projectIdentity,
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
                    WorkspaceRelativePath = $"{projectIdentity}/Source/Accounts/Register/Register.cs"
                }
            ]);
        return new DotNetProjectCompilation
        {
            Name = "Banking",
            Compilation = compilation,
            SourceContext = sourceContext,
            AuthoredSyntaxTrees = new HashSet<SyntaxTree> { tree }
        };
    }
}
