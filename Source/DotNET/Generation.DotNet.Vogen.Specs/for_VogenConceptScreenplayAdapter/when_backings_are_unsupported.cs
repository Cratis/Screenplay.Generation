// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_backings_are_unsupported : given.a_vogen_compilation
{
    AdapterContribution _contribution = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            "Concepts",
            new SourceFile(
                "/workspace/Concepts/Unsupported.cs",
                """
                namespace Concepts;
                public sealed class CustomBacking;
                [Vogen.ValueObject<char>] public partial struct CharacterCode;
                [Vogen.ValueObject<System.TimeOnly>] public partial struct TimeCode;
                [Vogen.ValueObject<CustomBacking>] public partial struct CustomCode;
                [Vogen.ValueObject<System.Collections.Generic.List<int>>] public partial struct CollectionCode;
                """));

        _contribution = Analyze(Project("Concepts.Project", compilation));
    }

    [Fact] void should_keep_every_concept_fact() => _contribution.Facts.OfType<ArtifactFact>().Select(_ => _.Definition.Name).ShouldContainOnly("CharacterCode", "CollectionCode", "CustomCode", "TimeCode");
    [Fact] void should_not_emit_representation_facts() => _contribution.Facts.OfType<ConceptRepresentationFact>().ShouldBeEmpty();
    [Fact] void should_never_fall_back_to_text() => _contribution.Facts.OfType<ConceptRepresentationFact>().Any(_ => _.Definition.Primitive == GenerationPrimitiveKind.Text).ShouldBeFalse();
    [Fact] void should_emit_one_vogen_diagnostic_per_concept() => _contribution.Diagnostics.Select(_ => _.Code).ShouldEqual(Enumerable.Repeat(VogenGenerationDiagnosticCodes.UnsupportedBackingType, 4));
    [Fact] void should_warn_about_representation_loss() => _contribution.Diagnostics.All(_ => _.Severity == GenerationDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_identify_the_exact_subject() => _contribution.Diagnostics.Single(_ => _.Message.Contains("CharacterCode", StringComparison.Ordinal)).Subject!.Value.ShouldEqual("dotnet://Concepts.Project/Concepts/Concepts.CharacterCode");
    [Fact] void should_anchor_the_diagnostic_at_the_authored_attribute() => _contribution.Diagnostics.Single(_ => _.Message.Contains("CharacterCode", StringComparison.Ordinal)).Source!.StartLine.ShouldEqual(3);
}
