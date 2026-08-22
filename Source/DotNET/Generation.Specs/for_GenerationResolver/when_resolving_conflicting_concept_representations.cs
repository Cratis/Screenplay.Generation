// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_conflicting_concept_representations : given.facts
{
    ResolvedApplicationGraph _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Concepts.AccountId" };
        _result = new GenerationResolver().Resolve(
        [
            Contribution(FirstAdapter, Representation("first", FirstAdapter, subject, GenerationPrimitiveKind.Uuid)),
            Contribution(SecondAdapter, Representation("second", SecondAdapter, subject, GenerationPrimitiveKind.Text))
        ]);
    }

    [Fact] void should_retain_both_representations() => _result.ConceptRepresentations.Single().Variants.Count.ShouldEqual(2);
    [Fact] void should_mark_the_representation_as_conflicted() => _result.ConceptRepresentations.Single().IsConflicted.ShouldBeTrue();
    [Fact] void should_report_the_conflict() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingConceptRepresentation);

    static ConceptRepresentationFact Representation(
        string id,
        AdapterIdentity adapter,
        SubjectId subject,
        GenerationPrimitiveKind primitive) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Definition = new ConceptRepresentationDefinition
        {
            Concept = subject,
            Kind = ConceptRepresentationKind.Primitive,
            Primitive = primitive
        },
        Evidence = new Evidence { Adapter = adapter, Strength = EvidenceStrength.Exact }
    };
}
