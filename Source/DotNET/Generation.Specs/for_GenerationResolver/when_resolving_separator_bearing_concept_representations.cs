// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_separator_bearing_concept_representations : given.facts
{
    ResolvedApplicationGraph _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Concepts.Status" };
        _result = new GenerationResolver().Resolve(
        [
            Contribution(FirstAdapter, Representation("first", FirstAdapter, subject, ["A", "B"])),
            Contribution(SecondAdapter, Representation("second", SecondAdapter, subject, ["A\u001fB"]))
        ]);
    }

    [Fact] void should_retain_both_structurally_different_representations() => _result.ConceptRepresentations.Single().Variants.Count.ShouldEqual(2);
    [Fact] void should_report_the_conflict() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingConceptRepresentation);

    static ConceptRepresentationFact Representation(
        string id,
        AdapterIdentity adapter,
        SubjectId subject,
        IReadOnlyList<string> values) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Definition = new ConceptRepresentationDefinition
        {
            Concept = subject,
            Kind = ConceptRepresentationKind.Enumeration,
            EnumerationValues = values
        },
        Evidence = new Evidence { Adapter = adapter, Strength = EvidenceStrength.Exact }
    };
}
