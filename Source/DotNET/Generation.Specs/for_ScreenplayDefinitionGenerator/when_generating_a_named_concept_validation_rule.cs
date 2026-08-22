// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_a_named_concept_validation_rule : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var concept = Concept("AccountNumber", GenerationPrimitiveKind.Text);
        var rule = Validation(
            "account-number-format",
            concept.Subject,
            "format",
            "BeValidAccountNumber",
            "Must be a valid account number",
            "Concepts\\Validation\\BeValidAccountNumber.cs");

        _result = Generator.Generate(
            [Contribution([.. concept.Facts, rule])],
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_print_the_named_predicate() => _result.Source.ShouldContain("rule BeValidAccountNumber");
    [Fact] void should_print_the_message() => _result.Source.ShouldContain("message \"Must be a valid account number\"");
    [Fact] void should_print_the_normalized_implementation_file() => _result.Source.ShouldContain("file Concepts/Validation/BeValidAccountNumber.cs");
    [Fact] void should_compile_the_generated_document() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain(GenerationDiagnosticCodes.DocumentDidNotCompile);
    [Fact] void should_roundtrip_stably() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain(GenerationDiagnosticCodes.UnstableRoundTrip);
}
