// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_duplicate_concept_representations : given.facts
{
    ResolvedApplicationGraph _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Concepts.AccountId" };
        _result = new GenerationResolver().Resolve(
        [
            Contribution(FirstAdapter, Representation("first", FirstAdapter, subject)),
            Contribution(SecondAdapter, Representation("second", SecondAdapter, subject))
        ]);
    }

    [Fact] void should_resolve_one_representation() => _result.ConceptRepresentations.Single().Variants.Count.ShouldEqual(1);
    [Fact] void should_merge_both_evidence_sources() => _result.ConceptRepresentations.Single().Variants.Single().Evidence.Count.ShouldEqual(2);
    [Fact] void should_not_report_a_conflict() => _result.Diagnostics.ShouldBeEmpty();

    static ConceptRepresentationFact Representation(string id, AdapterIdentity adapter, SubjectId subject) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Definition = new ConceptRepresentationDefinition
        {
            Concept = subject,
            Kind = ConceptRepresentationKind.Primitive,
            Primitive = GenerationPrimitiveKind.Uuid
        },
        Evidence = new Evidence { Adapter = adapter, Strength = EvidenceStrength.Exact }
    };
}
