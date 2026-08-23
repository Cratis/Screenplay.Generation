// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_mixed_invalid_public_discriminators : for_GenerationResolver.given.facts
{
    const int UndefinedValue = 731;
    GeneratedScreenplayDefinition _unknown = null!;
    GeneratedScreenplayDefinition _unknownReversed = null!;
    GeneratedScreenplayDefinition _undefined = null!;
    GeneratedScreenplayDefinition _undefinedReversed = null!;

    void Because()
    {
        (_unknown, _unknownReversed) = GenerateBothOrders(-1);
        (_undefined, _undefinedReversed) = GenerateBothOrders(UndefinedValue);
    }

    [Fact] void should_define_unknown_without_renumbering_public_fact_discriminators()
    {
        ((int)ArtifactKind.Unknown).ShouldEqual(-1);
        ((int)RelationshipKind.Unknown).ShouldEqual(-1);
        ((int)GenerationSliceKind.Unknown).ShouldEqual(-1);
        ((int)ConceptRepresentationKind.Unknown).ShouldEqual(-1);
        ((int)GenerationPrimitiveKind.Unknown).ShouldEqual(-1);
        ((int)ConceptAttributeKind.Unknown).ShouldEqual(-1);
        ((int)ConceptValidationRuleKind.Unknown).ShouldEqual(-1);
        ((int)EvidenceStrength.Unknown).ShouldEqual(-1);
    }

    [Fact] void should_keep_existing_discriminator_numeric_values()
    {
        ((int)ArtifactKind.ApplicationHost).ShouldEqual(0);
        ((int)RelationshipKind.Handles).ShouldEqual(0);
        ((int)GenerationSliceKind.StateChange).ShouldEqual(0);
        ((int)ConceptRepresentationKind.Primitive).ShouldEqual(0);
        ((int)GenerationPrimitiveKind.Uuid).ShouldEqual(0);
        ((int)ConceptAttributeKind.Named).ShouldEqual(0);
        ((int)ConceptValidationRuleKind.NamedPredicate).ShouldEqual(0);
        ((int)EvidenceStrength.Exact).ShouldEqual(0);
    }

    [Fact] void should_preserve_legacy_concept_attribute_record_equality()
    {
        var subject = Subject("attribute-equality");
        var legacyShape = new ConceptAttributeDefinition { Concept = subject, Name = "sensitive" };
        var typedShape = new ConceptAttributeDefinition
        {
            Concept = subject,
            Kind = ConceptAttributeKind.Named,
            Name = "sensitive"
        };

        legacyShape.ShouldEqual(typedShape);
    }

    [Fact] void should_report_every_unknown_discriminator_with_a_typed_unknown_outcome() =>
        AssertDiagnostics(_unknown, GenerationDiagnosticOutcome.Unknown);

    [Fact] void should_report_every_undefined_discriminator_with_a_typed_unsupported_outcome() =>
        AssertDiagnostics(_undefined, GenerationDiagnosticOutcome.Unsupported);

    [Fact] void should_omit_only_the_invalid_facts_and_keep_the_valid_artifact() =>
        AssertOnlyValidArtifactRemains(_unknown);

    [Fact] void should_keep_generating_the_valid_artifact_for_undefined_values() =>
        AssertOnlyValidArtifactRemains(_undefined);

    [Fact] void should_be_adapter_and_fact_order_independent_for_unknown_values() =>
        AssertSameResult(_unknown, _unknownReversed);

    [Fact] void should_be_adapter_and_fact_order_independent_for_undefined_values() =>
        AssertSameResult(_undefined, _undefinedReversed);

    [Fact] void should_not_fabricate_a_source_for_invalid_evidence_without_a_source()
    {
        var diagnostic = _unknown.Graph.Diagnostics.Single(_ => _.Subject == Subject("unknown-evidence"));
        diagnostic.Source.ShouldBeNull();
    }

    static (GeneratedScreenplayDefinition Forward, GeneratedScreenplayDefinition Reverse) GenerateBothOrders(int value)
    {
        var validFacts = ValidEvent();
        var invalidFacts = InvalidFacts(value);
        var generator = new ScreenplayDefinitionGenerator();
        var options = new ScreenplayGenerationOptions { Domain = "Banking" };
        var forward = generator.Generate(
            [
                Contribution(FirstAdapter, invalidFacts),
                Contribution(SecondAdapter, validFacts)
            ],
            options);
        var reverse = generator.Generate(
            [
                Contribution(SecondAdapter, [.. validFacts.AsEnumerable().Reverse()]),
                Contribution(FirstAdapter, [.. invalidFacts.AsEnumerable().Reverse()])
            ],
            options);

        return (forward, reverse);
    }

    static GenerationFact[] ValidEvent()
    {
        var artifact = new ArtifactKey { Subject = EventSubject, Kind = ArtifactKind.Event };
        var evidence = EvidenceFor(EventSubject, SecondAdapter);
        return
        [
            new ArtifactFact
            {
                Id = new FactId { Value = "valid:event" },
                Subject = EventSubject,
                Evidence = evidence,
                Definition = EventDefinition()
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "valid:placement" },
                Subject = EventSubject,
                Evidence = evidence,
                Artifact = artifact,
                Placement = new ArtifactPlacement
                {
                    Module = "Accounts",
                    Features = ["Opening"],
                    Slice = "Open",
                    SliceKind = GenerationSliceKind.StateChange
                }
            }
        ];
    }

    static GenerationFact[] InvalidFacts(int value)
    {
        var unknownArtifact = Subject("unknown-artifact");
        var unknownPlacementArtifact = Subject("unknown-placement-artifact");
        var unknownSlice = Subject("unknown-slice");
        var unknownRelationship = Subject("unknown-relationship");
        var unknownRepresentation = Subject("unknown-representation");
        var unknownPrimitive = Subject("unknown-primitive");
        var unknownAttribute = Subject("unknown-attribute");
        var unknownValidation = Subject("unknown-validation");
        var unknownEvidence = Subject("unknown-evidence");

        return
        [
            new ArtifactFact
            {
                Id = new FactId { Value = "invalid:artifact-kind" },
                Subject = unknownArtifact,
                Evidence = EvidenceFor(unknownArtifact, FirstAdapter),
                Definition = new ArtifactDefinition
                {
                    Key = new ArtifactKey { Subject = unknownArtifact, Kind = (ArtifactKind)value },
                    Name = "UnknownArtifact"
                }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "invalid:placement-artifact-kind" },
                Subject = unknownPlacementArtifact,
                Evidence = EvidenceFor(unknownPlacementArtifact, FirstAdapter),
                Artifact = new ArtifactKey { Subject = unknownPlacementArtifact, Kind = (ArtifactKind)value },
                Placement = Placement(GenerationSliceKind.StateChange)
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "invalid:slice-kind" },
                Subject = unknownSlice,
                Evidence = EvidenceFor(unknownSlice, FirstAdapter),
                Artifact = new ArtifactKey { Subject = unknownSlice, Kind = ArtifactKind.Event },
                Placement = Placement((GenerationSliceKind)value)
            },
            new RelationshipFact
            {
                Id = new FactId { Value = "invalid:relationship-kind" },
                Subject = unknownRelationship,
                Evidence = EvidenceFor(unknownRelationship, FirstAdapter),
                Definition = new RelationshipDefinition
                {
                    Key = new RelationshipKey
                    {
                        Kind = (RelationshipKind)value,
                        Source = unknownRelationship,
                        Target = EventSubject
                    }
                }
            },
            new ConceptRepresentationFact
            {
                Id = new FactId { Value = "invalid:representation-kind" },
                Subject = unknownRepresentation,
                Evidence = EvidenceFor(unknownRepresentation, FirstAdapter),
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = unknownRepresentation,
                    Kind = (ConceptRepresentationKind)value
                }
            },
            new ConceptRepresentationFact
            {
                Id = new FactId { Value = "invalid:primitive-kind" },
                Subject = unknownPrimitive,
                Evidence = EvidenceFor(unknownPrimitive, FirstAdapter),
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = unknownPrimitive,
                    Kind = ConceptRepresentationKind.Primitive,
                    Primitive = (GenerationPrimitiveKind)value
                }
            },
            new ConceptAttributeFact
            {
                Id = new FactId { Value = "invalid:attribute-kind" },
                Subject = unknownAttribute,
                Evidence = EvidenceFor(unknownAttribute, FirstAdapter),
                Definition = new ConceptAttributeDefinition
                {
                    Concept = unknownAttribute,
                    Kind = (ConceptAttributeKind)value,
                    Name = "sensitive"
                }
            },
            new ConceptValidationRuleFact
            {
                Id = new FactId { Value = "invalid:validation-kind" },
                Subject = unknownValidation,
                Evidence = EvidenceFor(unknownValidation, FirstAdapter),
                Definition = new ConceptValidationRuleDefinition
                {
                    Concept = unknownValidation,
                    RuleIdentity = "invalid",
                    Kind = (ConceptValidationRuleKind)value
                }
            },
            new ArtifactFact
            {
                Id = new FactId { Value = "invalid:evidence-strength" },
                Subject = unknownEvidence,
                Evidence = new Evidence
                {
                    Adapter = FirstAdapter,
                    Strength = (EvidenceStrength)value
                },
                Definition = new ArtifactDefinition
                {
                    Key = new ArtifactKey { Subject = unknownEvidence, Kind = ArtifactKind.Event },
                    Name = "UnknownEvidence"
                }
            }
        ];
    }

    static ArtifactPlacement Placement(GenerationSliceKind kind) => new()
    {
        Module = "Invalid",
        Features = ["Invalid"],
        Slice = "Invalid",
        SliceKind = kind
    };

    static Evidence EvidenceFor(SubjectId subject, AdapterIdentity adapter) => new()
    {
        Adapter = adapter,
        Strength = EvidenceStrength.Exact,
        Source = new SourceRange
        {
            Path = $"Invalid/{subject.Value[(subject.Value.LastIndexOf('/') + 1)..]}.cs",
            StartLine = 1,
            StartColumn = 1,
            EndLine = 1,
            EndColumn = 10
        }
    };

    static SubjectId Subject(string name) => new() { Value = $"dotnet://Invalid/{name}" };

    static void AssertDiagnostics(GeneratedScreenplayDefinition result, GenerationDiagnosticOutcome outcome)
    {
        result.Graph.Diagnostics.Count.ShouldEqual(9);
        AssertDiagnostic(result, "unknown-artifact", GenerationDiagnosticCodes.UnsupportedArtifactKind, outcome);
        AssertDiagnostic(result, "unknown-placement-artifact", GenerationDiagnosticCodes.UnsupportedArtifactKind, outcome);
        AssertDiagnostic(result, "unknown-slice", GenerationDiagnosticCodes.UnsupportedSliceKind, outcome);
        AssertDiagnostic(result, "unknown-relationship", GenerationDiagnosticCodes.UnsupportedRelationshipKind, outcome);
        AssertDiagnostic(result, "unknown-representation", GenerationDiagnosticCodes.UnsupportedConceptRepresentationKind, outcome);
        AssertDiagnostic(result, "unknown-primitive", GenerationDiagnosticCodes.UnsupportedPrimitiveKind, outcome);
        AssertDiagnostic(result, "unknown-attribute", GenerationDiagnosticCodes.UnsupportedConceptAttributeKind, outcome);
        AssertDiagnostic(result, "unknown-validation", GenerationDiagnosticCodes.UnsupportedConceptValidationRuleKind, outcome);
        AssertDiagnostic(result, "unknown-evidence", GenerationDiagnosticCodes.UnsupportedEvidenceStrength, outcome, false);
    }

    static void AssertDiagnostic(
        GeneratedScreenplayDefinition result,
        string subjectName,
        string code,
        GenerationDiagnosticOutcome outcome,
        bool hasSource = true)
    {
        var subject = Subject(subjectName);
        var diagnostic = result.Graph.Diagnostics.Single(_ => _.Subject == subject);
        diagnostic.Code.ShouldEqual(code);
        diagnostic.Outcome.ShouldEqual(outcome);
        diagnostic.Subject.ShouldEqual(subject);
        if (hasSource)
        {
            diagnostic.Source.ShouldEqual(EvidenceFor(subject, FirstAdapter).Source);
        }
        else
        {
            diagnostic.Source.ShouldBeNull();
        }
    }

    static void AssertOnlyValidArtifactRemains(GeneratedScreenplayDefinition result)
    {
        result.IsSuccess.ShouldBeTrue();
        result.Graph.Artifacts.Single().Key.ShouldEqual(new ArtifactKey { Subject = EventSubject, Kind = ArtifactKind.Event });
        result.Graph.Placements.Single().Artifact.ShouldEqual(new ArtifactKey { Subject = EventSubject, Kind = ArtifactKind.Event });
        result.Graph.Relationships.ShouldBeEmpty();
        result.Graph.ConceptRepresentations.ShouldBeEmpty();
        result.Graph.ConceptAttributes.ShouldBeEmpty();
        result.Graph.ConceptValidationRules.ShouldBeEmpty();
        result.Source.ShouldContain("event AccountOpened");
        result.Source.ShouldNotContain("UnknownArtifact");
        result.Source.ShouldNotContain("UnknownEvidence");
    }

    static void AssertSameResult(GeneratedScreenplayDefinition first, GeneratedScreenplayDefinition second)
    {
        second.Source.ShouldEqual(first.Source);
        JsonSerializer.Serialize(second.Graph).ShouldEqual(JsonSerializer.Serialize(first.Graph));
        JsonSerializer.Serialize(second.Diagnostics).ShouldEqual(JsonSerializer.Serialize(first.Diagnostics));
    }
}
