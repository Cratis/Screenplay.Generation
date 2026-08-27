// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_calculating_concept_fact_dispositions : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var accountNumber = Concept("AccountNumber", GenerationPrimitiveKind.Text);
        var attribute = new ConceptAttributeFact
        {
            Id = new FactId { Value = "attribute:account-number" },
            Subject = accountNumber.Subject,
            Evidence = Exact(),
            Definition = new ConceptAttributeDefinition
            {
                Concept = accountNumber.Subject,
                Name = "sensitive",
                Reason = "Routes payments"
            }
        };
        var validation = Validation(
            "validation:account-number",
            accountNumber.Subject,
            "format",
            "BeValidAccountNumber");
        var invalidAttribute = attribute with
        {
            Id = new FactId { Value = "attribute:invalid" },
            Definition = attribute.Definition with { Name = "Not Valid" }
        };
        var conflictedSubject = new SubjectId { Value = "dotnet://Banking/Concepts.ExternalCode" };
        var conflictedArtifact = new ArtifactFact
        {
            Id = new FactId { Value = "concept:conflicted" },
            Subject = conflictedSubject,
            Evidence = Exact(),
            Definition = new ArtifactDefinition
            {
                Key = new ArtifactKey { Subject = conflictedSubject, Kind = ArtifactKind.Concept },
                Name = "ExternalCode"
            }
        };
        var firstRepresentation = new ConceptRepresentationFact
        {
            Id = new FactId { Value = "representation:first" },
            Subject = conflictedSubject,
            Evidence = Exact(),
            Definition = new ConceptRepresentationDefinition
            {
                Concept = conflictedSubject,
                Kind = ConceptRepresentationKind.Primitive,
                Primitive = GenerationPrimitiveKind.Text
            }
        };
        var secondRepresentation = firstRepresentation with
        {
            Id = new FactId { Value = "representation:second" },
            Definition = firstRepresentation.Definition with { Primitive = GenerationPrimitiveKind.Uuid }
        };

        _result = Generator.Generate(
            Snapshot(Completed(
                Adapter,
                [
                    .. accountNumber.Facts,
                    attribute,
                    validation,
                    invalidAttribute,
                    conflictedArtifact,
                    firstRepresentation,
                    secondRepresentation
                ])),
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_lower_the_concept_artifact() => Disposition("concept:AccountNumber").ShouldEqual(GenerationFactDisposition.Lowered);
    [Fact] void should_lower_the_concept_representation() => Disposition("concept-representation:AccountNumber").ShouldEqual(GenerationFactDisposition.Lowered);
    [Fact] void should_lower_the_emitted_attribute() => Disposition("attribute:account-number").ShouldEqual(GenerationFactDisposition.Lowered);
    [Fact] void should_lower_the_emitted_validation() => Disposition("validation:account-number").ShouldEqual(GenerationFactDisposition.Lowered);
    [Fact] void should_omit_the_invalid_attribute_with_its_stable_diagnostic() => DiagnosticCode("attribute:invalid").ShouldEqual(GenerationDiagnosticCodes.UnsupportedConceptAttribute);
    [Fact] void should_classify_the_first_conflicting_representation() => Disposition("representation:first").ShouldEqual(GenerationFactDisposition.Conflicted);
    [Fact] void should_classify_the_second_conflicting_representation() => Disposition("representation:second").ShouldEqual(GenerationFactDisposition.Conflicted);
    [Fact] void should_omit_the_concept_whose_representation_conflicted() => Disposition("concept:conflicted").ShouldEqual(GenerationFactDisposition.OmittedWithDiagnostic);
    [Fact] void should_not_leave_any_fact_unknown() => _result.AdapterRun!.Facts.Any(record => record.Disposition == GenerationFactDisposition.Unknown).ShouldBeFalse();

    GenerationFactDisposition Disposition(string id) => Record(id).Disposition;

    string DiagnosticCode(string id) => Record(id).Diagnostics[0].Code;

    GenerationFactRecord Record(string id) => _result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == id);

    Evidence Exact() => new() { Adapter = Adapter, Strength = EvidenceStrength.Exact };
}
