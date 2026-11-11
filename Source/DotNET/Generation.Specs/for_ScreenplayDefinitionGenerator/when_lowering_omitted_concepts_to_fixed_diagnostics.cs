// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_omitted_concepts_to_fixed_diagnostics : given.a_lowering_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        _result = Generator.Generate(
            [Contribution(
                [
                    .. ConceptArtifact("NoRepresentation", 1),
                    .. ConceptArtifact("PrimitiveWithoutKind", 2),
                    .. Representation("PrimitiveWithoutKind", ConceptRepresentationKind.Primitive, null, []),
                    .. ConceptArtifact("PrimitiveWithValues", 3),
                    .. Representation("PrimitiveWithValues", ConceptRepresentationKind.Primitive, GenerationPrimitiveKind.Text, ["Alpha"]),
                    .. ConceptArtifact("EnumerationWithPrimitive", 4),
                    .. Representation("EnumerationWithPrimitive", ConceptRepresentationKind.Enumeration, GenerationPrimitiveKind.Text, ["Alpha"]),
                    .. ConceptArtifact("EnumerationWithoutValues", 5),
                    .. Representation("EnumerationWithoutValues", ConceptRepresentationKind.Enumeration, null, []),
                    .. ConceptArtifact("DuplicateValues", 6),
                    .. Representation("DuplicateValues", ConceptRepresentationKind.Enumeration, null, ["Ready", "ready"]),
                    .. ConceptArtifact("ReservedValue", 7),
                    .. Representation("ReservedValue", ConceptRepresentationKind.Enumeration, null, ["File"]),
                    .. ConceptArtifact("EmptyValue", 8),
                    .. Representation("EmptyValue", ConceptRepresentationKind.Enumeration, null, [""]),
                    .. ConceptArtifact("NotAnIdentifierValue", 9),
                    .. Representation("NotAnIdentifierValue", ConceptRepresentationKind.Enumeration, null, ["Two Words"]),
                    .. ConceptArtifact("Contested", 10, "Shared"),
                    .. Representation("Contested", ConceptRepresentationKind.Primitive, GenerationPrimitiveKind.Text, []),
                    .. ConceptArtifact("Contesting", 11, "Shared"),
                    .. Representation("Contesting", ConceptRepresentationKind.Primitive, GenerationPrimitiveKind.Uuid, []),
                    .. ConceptArtifact("Survivor", 12),
                    .. Representation("Survivor", ConceptRepresentationKind.Primitive, GenerationPrimitiveKind.Boolean, []),
                    InvalidAttribute("Survivor", "Not Valid", 13),
                    InvalidAttribute("Survivor", "1st", 14),
                    Validation("rule-missing-predicate", SubjectOf("Concepts.Survivor"), "no-predicate", null),
                    Validation("rule-bad-predicate", SubjectOf("Concepts.Survivor"), "bad-predicate", "1Bad"),
                    Validation("rule-bad-file", SubjectOf("Concepts.Survivor"), "bad-file", "BeGood", implementationFile: " "),
                    Validation("rule-multiline-file", SubjectOf("Concepts.Survivor"), "multiline-file", "BeGood", implementationFile: "a\r\nb.cs")
                ])],
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact]
    void should_emit_only_the_surviving_concept() => _result.Source.ReplaceLineEndings("\n").ShouldEqual("domain Banking\n\nconcept Survivor : Bool\n");

    [Fact]
    void should_report_the_complete_fixed_diagnostics() => Describe(_result.Diagnostics).ShouldEqual(
        """
        GEN0012 | Warning | Unknown | dotnet://Banking/Concepts.NoRepresentation | - | Concept 'NoRepresentation' has no proven representation and was omitted
        GEN0013 | Warning | Unsupported | dotnet://Banking/Concepts.EmptyValue | Concepts/EmptyValue.cs:100:1-100:20 | Concept 'EmptyValue' has empty or duplicate enumeration values after Screenplay naming and was omitted
        GEN0013 | Warning | Unsupported | dotnet://Banking/Concepts.ReservedValue | Concepts/ReservedValue.cs:100:1-100:20 | Concept 'ReservedValue' has empty or duplicate enumeration values after Screenplay naming and was omitted
        GEN0013 | Warning | Unsupported | dotnet://Banking/Concepts.DuplicateValues | Concepts/DuplicateValues.cs:100:1-100:20 | Concept 'DuplicateValues' has empty or duplicate enumeration values after Screenplay naming and was omitted
        GEN0013 | Warning | Unsupported | dotnet://Banking/Concepts.PrimitiveWithValues | Concepts/PrimitiveWithValues.cs:100:1-100:20 | Concept 'PrimitiveWithValues' has an invalid or unsupported Primitive representation and was omitted
        GEN0013 | Warning | Unsupported | dotnet://Banking/Concepts.NotAnIdentifierValue | Concepts/NotAnIdentifierValue.cs:100:1-100:20 | Concept 'NotAnIdentifierValue' has empty or duplicate enumeration values after Screenplay naming and was omitted
        GEN0013 | Warning | Unsupported | dotnet://Banking/Concepts.PrimitiveWithoutKind | Concepts/PrimitiveWithoutKind.cs:100:1-100:20 | Concept 'PrimitiveWithoutKind' has an invalid or unsupported Primitive representation and was omitted
        GEN0013 | Warning | Unsupported | dotnet://Banking/Concepts.EnumerationWithPrimitive | Concepts/EnumerationWithPrimitive.cs:100:1-100:20 | Concept 'EnumerationWithPrimitive' has an invalid or unsupported Enumeration representation and was omitted
        GEN0013 | Warning | Unsupported | dotnet://Banking/Concepts.EnumerationWithoutValues | Concepts/EnumerationWithoutValues.cs:100:1-100:20 | Concept 'EnumerationWithoutValues' has an invalid or unsupported Enumeration representation and was omitted
        GEN0019 | Warning | Unsupported | dotnet://Banking/Concepts.Survivor | Concepts/Survivor.cs:13:1-13:20 | Concept 'Survivor' has invalid attribute name 'Not Valid', which was omitted
        GEN0019 | Warning | Unsupported | dotnet://Banking/Concepts.Survivor | Concepts/Survivor.cs:14:1-14:20 | Concept 'Survivor' has invalid attribute name '1st', which was omitted
        GEN0021 | Warning | Unsupported | dotnet://Banking/Concepts.Survivor | - | Concept 'Survivor' validation rule 'no-predicate' has invalid or missing required data and was omitted
        GEN0021 | Warning | Unsupported | dotnet://Banking/Concepts.Survivor | - | Concept 'Survivor' validation rule 'bad-predicate' has invalid or missing required data and was omitted
        GEN0021 | Warning | Unsupported | dotnet://Banking/Concepts.Survivor | - | Concept 'Survivor' validation rule 'multiline-file' has invalid or missing required data and was omitted
        GEN0021 | Warning | Unsupported | dotnet://Banking/Concepts.Survivor | - | Concept 'Survivor' validation rule 'bad-file' has invalid or missing required data and was omitted
        GEN0015 | Error | Conflict | dotnet://Banking/Concepts.Contested | - | Concept name 'Shared' is required by 2 distinct source subjects
        """);

    GenerationFact[] ConceptArtifact(string name, int line, string? conflictName = null)
    {
        var subject = SubjectOf($"Concepts.{name}");
        return
        [
            new ArtifactFact
            {
                Id = new FactId { Value = $"concept:{name}" },
                Subject = subject,
                Definition = new ArtifactDefinition
                {
                    Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept },
                    Name = conflictName ?? name
                },
                Evidence = EvidenceAt($"Concepts/{name}.cs", line)
            }
        ];
    }

    GenerationFact[] Representation(string name, ConceptRepresentationKind kind, GenerationPrimitiveKind? primitive, string[] values)
    {
        var subject = SubjectOf($"Concepts.{name}");
        return
        [
            new ConceptRepresentationFact
            {
                Id = new FactId { Value = $"representation:{name}" },
                Subject = subject,
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = subject,
                    Kind = kind,
                    Primitive = primitive,
                    EnumerationValues = values
                },
                Evidence = EvidenceAt($"Concepts/{name}.cs", 100)
            }
        ];
    }

    ConceptAttributeFact InvalidAttribute(string concept, string name, int line)
    {
        var subject = SubjectOf($"Concepts.{concept}");
        return new ConceptAttributeFact
        {
            Id = new FactId { Value = $"attribute:{name}" },
            Subject = subject,
            Definition = new ConceptAttributeDefinition { Concept = subject, Name = name },
            Evidence = EvidenceAt($"Concepts/{concept}.cs", line)
        };
    }
}
