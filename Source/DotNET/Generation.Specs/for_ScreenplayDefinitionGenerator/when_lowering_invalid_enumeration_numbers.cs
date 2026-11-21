// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_invalid_enumeration_numbers : given.a_lowering_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        _result = Generator.Generate(
            [Contribution([.. Invalid("Mismatched", [7]), .. Invalid("Duplicate", [7, 7]), .. Invalid("Primitive", [], true)])],
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_omit_all_invalid_representations() => _result.Source.ShouldNotContain("concept ");
    [Fact] void should_diagnose_each_invalid_representation() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.UnsupportedConceptRepresentation).ShouldEqual(3);

    GenerationFact[] Invalid(string name, IReadOnlyList<int> numbers, bool primitive = false)
    {
        var concept = Concept(name, GenerationPrimitiveKind.WholeNumber);
        return
        [
            concept.Facts.OfType<ArtifactFact>().Single(),
            new ConceptRepresentationFact
            {
                Id = new FactId { Value = $"{name}:representation" },
                Subject = concept.Subject,
                Evidence = EvidenceAt($"Concepts/{name}.cs", 2),
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = concept.Subject,
                    Kind = primitive ? ConceptRepresentationKind.Primitive : ConceptRepresentationKind.Enumeration,
                    Primitive = primitive ? GenerationPrimitiveKind.WholeNumber : null,
                    EnumerationValues = primitive ? [] : ["Open", "Closed"],
                    EnumerationNumbers = numbers
                }
            }
        ];
    }
}
