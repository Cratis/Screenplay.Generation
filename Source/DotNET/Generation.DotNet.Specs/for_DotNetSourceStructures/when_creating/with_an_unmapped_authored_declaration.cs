// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructures.when_creating;

public class with_an_unmapped_authored_declaration : given.a_compilation
{
    DotNetSourceStructureSnapshot _snapshot = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            new SourceFile(
                "/physical-checkout/Banking/Source/Accounts/Register/Register.cs",
                "namespace Banking.Accounts.Register; public partial record Register;"),
            new SourceFile(
                "/physical-checkout/Banking/Source/Accounts/Register/Register.Validation.cs",
                "namespace Banking.Accounts.Register; public partial record Register;"));
        var trees = compilation.SyntaxTrees.ToArray();
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
                    SyntaxTree = trees[0],
                    ProjectRelativePath = "Source/Accounts/Register/Register.cs",
                    WorkspaceRelativePath = "apps/Banking/Source/Accounts/Register/Register.cs"
                }
            ]);
        var project = new DotNetProjectCompilation
        {
            Name = "Banking",
            Compilation = compilation,
            SourceContext = sourceContext,
            AuthoredSyntaxTrees = trees.ToHashSet()
        };

        _snapshot = DotNetSourceStructures.Create(new DotNetAnalysisContext([project]));
    }

    [Fact] void should_fail_closed() => _snapshot.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_contribute_a_partial_structure() => _snapshot.Structures.ShouldBeEmpty();
    [Fact] void should_report_the_missing_mapping() => _snapshot.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.MissingSourceMapping);
    [Fact] void should_retain_the_affected_subject() => _snapshot.Diagnostics.Single().Subject.ShouldNotBeNull();
    [Fact] void should_not_expose_the_physical_checkout() => _snapshot.Diagnostics.Single().Message.Contains("physical-checkout", StringComparison.Ordinal).ShouldBeFalse();
}
