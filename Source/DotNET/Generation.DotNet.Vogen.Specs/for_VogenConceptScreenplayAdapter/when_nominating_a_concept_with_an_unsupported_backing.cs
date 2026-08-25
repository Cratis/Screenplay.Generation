// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_nominating_a_concept_with_an_unsupported_backing : given.a_vogen_compilation
{
    IReadOnlyList<GenerationFact> _facts = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            "Ordering",
            new SourceFile(
                "/workspace/Concepts/OrderNumber.cs",
                """
                namespace Ordering.Concepts;
                public sealed class CustomBacking;
                public readonly record struct OrderNumber(CustomBacking Value);
                """));
        var project = Project("Ordering", compilation);
        var wrapper = compilation.GetTypeByMetadataName("Ordering.Concepts.OrderNumber");
        var backing = compilation.GetTypeByMetadataName("Ordering.Concepts.CustomBacking");
        var subject = project.SubjectForType(wrapper);
        var evidence = DotNetSource.EvidenceFor(
            wrapper,
            new AdapterIdentity { Id = "registered-values", Version = "1.0.0" },
            project,
            EvidenceStrength.Configured);

        _facts = DotNetConceptFacts.Emit(wrapper, backing, subject, evidence);
    }

    [Fact] void should_preserve_the_concept_nomination() => _facts.OfType<ArtifactFact>().Count().ShouldEqual(1);
    [Fact] void should_not_guess_a_primitive_representation() => _facts.OfType<ConceptRepresentationFact>().ShouldBeEmpty();
}
