// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_a_concept_fact_with_a_different_subject : given.facts
{
    ResolvedApplicationGraph _result = null!;

    void Because()
    {
        var asserted = new SubjectId { Value = "dotnet://Banking/Concepts.Asserted" };
        var defined = new SubjectId { Value = "dotnet://Banking/Concepts.Defined" };
        _result = new GenerationResolver().Resolve(
        [
            Contribution(
                FirstAdapter,
                new ConceptRepresentationFact
                {
                    Id = new FactId { Value = "representation" },
                    Subject = asserted,
                    Definition = new ConceptRepresentationDefinition
                    {
                        Concept = defined,
                        Kind = ConceptRepresentationKind.Primitive,
                        Primitive = GenerationPrimitiveKind.Uuid
                    },
                    Evidence = new Evidence { Adapter = FirstAdapter, Strength = EvidenceStrength.Exact }
                })
        ]);
    }

    [Fact] void should_reject_the_representation() => _result.ConceptRepresentations.ShouldBeEmpty();
    [Fact] void should_report_the_invalid_fact() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(GenerationDiagnosticCodes.InvalidConceptFact);
}
