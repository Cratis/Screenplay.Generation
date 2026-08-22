// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_an_invalid_concept_attribute : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var concept = Concept("OrderId", GenerationPrimitiveKind.Uuid);
        _result = Generator.Generate(
        [
            Contribution(
                [
                    .. concept.Facts,
                    new ConceptAttributeFact
                    {
                        Id = new FactId { Value = "invalid-attribute" },
                        Subject = concept.Subject,
                        Definition = new ConceptAttributeDefinition { Concept = concept.Subject, Name = "Not Valid" },
                        Evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact }
                    }
                ])
        ],
        new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_report_the_invalid_attribute() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(GenerationDiagnosticCodes.UnsupportedConceptAttribute);
    [Fact] void should_keep_the_concept_without_the_attribute() => _result.Source.ShouldContain("concept OrderId : Uuid");
    [Fact] void should_not_emit_the_invalid_attribute() => _result.Source.ShouldNotContain("@Not Valid");
}
