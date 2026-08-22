// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_a_concept_with_conflicting_validation_rules : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var concept = Concept("AccountNumber", GenerationPrimitiveKind.Text);
        var attribute = new ConceptAttributeFact
        {
            Id = new FactId { Value = "account-number-sensitive" },
            Subject = concept.Subject,
            Definition = new ConceptAttributeDefinition
            {
                Concept = concept.Subject,
                Name = "sensitive",
                Reason = "Used to route payments"
            },
            Evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact }
        };
        var first = Validation("first-format", concept.Subject, "format", "BeValidAccountNumber");
        var second = Validation("second-format", concept.Subject, "format", "BeFormattedAccountNumber");

        _result = Generator.Generate(
            [Contribution([.. concept.Facts, attribute, first, second])],
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_report_the_rule_conflict() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingConceptValidationRule);
    [Fact] void should_keep_the_concept_representation() => _result.Source.ShouldContain("concept AccountNumber : String");
    [Fact] void should_keep_the_concept_attribute() => _result.Source.ShouldContain("@sensitive");
    [Fact] void should_keep_the_concept_attribute_reason() => _result.Source.ShouldContain("sensitive reason \"Used to route payments\"");
    [Fact] void should_omit_the_conflicting_rule() => _result.Source.ShouldNotContain("rule Be");
}
