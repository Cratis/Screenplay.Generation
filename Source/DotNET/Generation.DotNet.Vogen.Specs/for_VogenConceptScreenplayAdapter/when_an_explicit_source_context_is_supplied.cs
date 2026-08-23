// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_an_explicit_source_context_is_supplied : given.a_vogen_compilation
{
    AdapterContribution _contribution = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            "Banking",
            new SourceFile(
                "/checkout/apps/Banking/Concepts.cs",
                """
                namespace Banking;
                [Vogen.ValueObject<System.Guid>]
                public readonly partial record struct OrderId
                {
                    private static Vogen.Validation Validate(System.Guid value) => Vogen.Validation.Ok;
                }
                """));
        var tree = compilation.SyntaxTrees.Single();
        var sourceContext = DotNetSourcePaths.Create(
            "Banking/Banking",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = tree,
                    ProjectRelativePath = "Concepts.cs",
                    WorkspaceRelativePath = "apps/Banking/Concepts.cs"
                }
            ]);

        _contribution = Analyze(Project(
            "Banking.Project",
            compilation,
            "/checkout",
            compilation.SyntaxTrees,
            sourceContext));
    }

    [Fact] void should_keep_the_display_path_in_the_artifact() => ConceptNamed(_contribution, "OrderId").Definition.File.ShouldEqual("Concepts.cs");
    [Fact] void should_keep_the_display_path_in_the_validation_implementation() => _contribution.Facts.OfType<ConceptValidationRuleFact>().Single().Definition.ImplementationFile.ShouldEqual("Concepts.cs");
    [Fact] void should_attach_the_stable_file_identity_to_every_fact() => _contribution.Facts.All(_ => _.Evidence.Source.FileIdentity == new SourceFileIdentity { Project = "Banking/Banking", Path = "Concepts.cs" }).ShouldBeTrue();
}
