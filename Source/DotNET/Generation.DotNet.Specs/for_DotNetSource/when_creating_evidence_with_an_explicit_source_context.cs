// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_creating_evidence_with_an_explicit_source_context : given.a_compilation
{
    Evidence _evidence = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/checkout/apps/Banking/Account.cs",
            """
            namespace Banking;
            public record Account(System.Guid Id);
            """));
        var tree = compilation.SyntaxTrees.Single();
        var sourceContext = DotNetSourcePaths.Create(
            "Banking/Banking",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Workspace,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = tree,
                    ProjectRelativePath = "Account.cs",
                    WorkspaceRelativePath = "apps/Banking/Account.cs"
                }
            ]);
        var project = new DotNetProjectCompilation
        {
            Name = "Banking",
            ProjectPath = "/checkout/apps/Banking/Banking.csproj",
            SourceRoot = "/checkout",
            SourceContext = sourceContext,
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
        };
        var type = TypeNamed(compilation, "Banking.Account");
        var adapter = new AdapterIdentity { Id = "marten", Version = "1.0.0" };

        _evidence = DotNetSource.EvidenceFor(type, adapter, project, EvidenceStrength.Exact, "Mapped evidence");
    }

    [Fact] void should_use_the_declared_display_path() => _evidence.Source.Path.ShouldEqual("apps/Banking/Account.cs");
    [Fact] void should_keep_the_stable_project_identity() => _evidence.Source.FileIdentity.Project.ShouldEqual("Banking/Banking");
    [Fact] void should_keep_the_project_relative_identity_path() => _evidence.Source.FileIdentity.Path.ShouldEqual("Account.cs");
}
