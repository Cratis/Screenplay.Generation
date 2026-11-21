// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_explicit_enumeration_numbers : given.a_lowering_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var concept = Concept("Status", GenerationPrimitiveKind.WholeNumber);
        var representation = new ConceptRepresentationFact
        {
            Id = new FactId { Value = "status:representation" },
            Subject = concept.Subject,
            Definition = new ConceptRepresentationDefinition
            {
                Concept = concept.Subject,
                Kind = ConceptRepresentationKind.Enumeration,
                EnumerationValues = ["Maximum", "Negative", "Zero", "Minimum", "Sparse"],
                EnumerationNumbers = [int.MaxValue, -7, 0, int.MinValue, 42]
            },
            Evidence = EvidenceAt("Concepts/Status.cs", 2)
        };
        _result = Generator.Generate(
            [Contribution([concept.Facts.OfType<ArtifactFact>().Single(), representation])],
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_verify_the_numbered_document() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_declaration_order_and_int32_numbers() => _result.Source.ShouldContain("maximum = 2147483647\n  negative = -7\n  zero = 0\n  minimum = -2147483648\n  sparse = 42");
    [Fact] void should_report_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
