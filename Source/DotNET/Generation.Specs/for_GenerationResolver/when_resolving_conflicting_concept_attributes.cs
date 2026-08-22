// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_conflicting_concept_attributes : given.facts
{
    ResolvedApplicationGraph _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Concepts.AccountId" };
        _result = new GenerationResolver().Resolve(
        [
            Contribution(FirstAdapter, Attribute("first", FirstAdapter, subject, "First reason")),
            Contribution(SecondAdapter, Attribute("second", SecondAdapter, subject, "Second reason"))
        ]);
    }

    [Fact] void should_retain_both_attribute_definitions() => _result.ConceptAttributes.Single().Variants.Count.ShouldEqual(2);
    [Fact] void should_mark_the_attribute_as_conflicted() => _result.ConceptAttributes.Single().IsConflicted.ShouldBeTrue();
    [Fact] void should_report_the_conflict() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingConceptAttribute);

    static ConceptAttributeFact Attribute(string id, AdapterIdentity adapter, SubjectId subject, string reason) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Definition = new ConceptAttributeDefinition { Concept = subject, Name = "pii", Reason = reason },
        Evidence = new Evidence { Adapter = adapter, Strength = EvidenceStrength.Exact }
    };
}
