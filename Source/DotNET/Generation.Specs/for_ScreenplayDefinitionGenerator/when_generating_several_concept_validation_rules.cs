// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_several_concept_validation_rules : given.a_generator
{
    GeneratedScreenplayDefinition _first = null!;
    GeneratedScreenplayDefinition _second = null!;

    void Because()
    {
        var concept = Concept("AccountNumber", GenerationPrimitiveKind.Text);
        GenerationFact[] rules =
        [
            Validation("last", concept.Subject, "z-last", "BeLast"),
            Validation("first", concept.Subject, "a-first", "BeFirst"),
            Validation("middle", concept.Subject, "m-middle", "BeMiddle")
        ];
        var options = new ScreenplayGenerationOptions { Domain = "Banking" };

        _first = Generator.Generate([Contribution([.. concept.Facts, .. rules])], options);
        _second = Generator.Generate([Contribution([.. rules.AsEnumerable().Reverse(), .. concept.Facts.AsEnumerable().Reverse()])], options);
    }

    [Fact] void should_succeed_in_the_first_order() => _first.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_in_the_second_order() => _second.IsSuccess.ShouldBeTrue();
    [Fact] void should_generate_identical_source_when_shuffled() => _second.Source.ShouldEqual(_first.Source);
    [Fact] void should_order_rules_by_stable_rule_identity() =>
        (_first.Source.IndexOf("rule BeFirst", StringComparison.Ordinal) <
         _first.Source.IndexOf("rule BeMiddle", StringComparison.Ordinal) &&
         _first.Source.IndexOf("rule BeMiddle", StringComparison.Ordinal) <
         _first.Source.IndexOf("rule BeLast", StringComparison.Ordinal)).ShouldBeTrue();
}
