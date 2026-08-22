// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_concepts : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var orderId = Concept("OrderId", GenerationPrimitiveKind.Uuid);
        var statusSubject = new SubjectId { Value = "dotnet://Banking/Concepts.OrderStatus" };
        var evidence = new Evidence
        {
            Adapter = Adapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange { Path = "Concepts/OrderStatus.cs", StartLine = 1, StartColumn = 1, EndLine = 1, EndColumn = 1 }
        };
        GenerationFact[] status =
        [
            new ArtifactFact
            {
                Id = new FactId { Value = "concept:OrderStatus" },
                Subject = statusSubject,
                Definition = new ArtifactDefinition
                {
                    Key = new ArtifactKey { Subject = statusSubject, Kind = ArtifactKind.Concept },
                    Name = "OrderStatus",
                    File = "Concepts/OrderStatus.cs"
                },
                Evidence = evidence
            },
            new ConceptRepresentationFact
            {
                Id = new FactId { Value = "concept-representation:OrderStatus" },
                Subject = statusSubject,
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = statusSubject,
                    Kind = ConceptRepresentationKind.Enumeration,
                    EnumerationValues = ["Pending", "Accepted"]
                },
                Evidence = evidence
            }
        ];
        var placed = Event(
            "OrderPlaced",
            "Place",
            Property("orderId", "UnresolvedOrderId", orderId.Subject),
            Property("status", "UnresolvedOrderStatus", statusSubject));

        _result = Generator.Generate(
            [Contribution([.. orderId.Facts, .. status, .. placed])],
            new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_generate_the_primitive_concept() => _result.Source.ShouldContain("concept OrderId : Uuid");
    [Fact] void should_generate_the_enumeration_concept() => _result.Source.ShouldContain("concept OrderStatus : Enum");
    [Fact] void should_generate_the_pending_enumeration_value() => _result.Source.ShouldContain("pending");
    [Fact] void should_generate_the_accepted_enumeration_value() => _result.Source.ShouldContain("accepted");
    [Fact] void should_keep_the_concept_file() => _result.Source.ShouldContain("file Concepts/OrderId.cs");
    [Fact] void should_resolve_the_exact_concept_subject_for_the_event_property() => _result.Source.ShouldContain("orderId OrderId");
    [Fact] void should_resolve_the_exact_enumeration_subject_for_the_event_property() => _result.Source.ShouldContain("status OrderStatus");
    [Fact] void should_not_require_concept_placement() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain(GenerationDiagnosticCodes.IncompleteArtifact);
}
