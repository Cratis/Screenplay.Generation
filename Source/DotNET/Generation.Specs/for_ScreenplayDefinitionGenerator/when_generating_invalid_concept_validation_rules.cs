// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_invalid_concept_validation_rules : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var concept = Concept("AccountNumber", GenerationPrimitiveKind.Text);
        var missingIdentity = Validation("missing-identity", concept.Subject, string.Empty, "BeValidAccountNumber");
        var missingPredicate = Validation("missing-predicate", concept.Subject, "missing-predicate", null);
        missingPredicate = missingPredicate with
        {
            Evidence = missingPredicate.Evidence with
            {
                Source = new SourceRange
                {
                    Path = "Concepts/AccountNumber.cs",
                    StartLine = 9,
                    StartColumn = 1,
                    EndLine = 9,
                    EndColumn = 25
                }
            }
        };
        var invalidPredicate = Validation("invalid-predicate", concept.Subject, "format", "Not a predicate");
        var invalidImplementationFile = Validation("invalid-file", concept.Subject, "file", "BeSafe", implementationFile: "Concepts/Bad\nfile.cs");

        _result = Generator.Generate(
            [Contribution([.. concept.Facts, missingIdentity, missingPredicate, invalidPredicate, invalidImplementationFile])],
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_report_each_unsupported_rule() => _result.Diagnostics.Count(_ => _.Code == GenerationDiagnosticCodes.UnsupportedConceptValidationRule).ShouldEqual(4);
    [Fact] void should_preserve_available_rule_source() => _result.Diagnostics.Any(_ => _.Source?.Path == "Concepts/AccountNumber.cs").ShouldBeTrue();
    [Fact] void should_keep_the_concept() => _result.Source.ShouldContain("concept AccountNumber : String");
    [Fact] void should_omit_the_invalid_predicate() => _result.Source.ShouldNotContain("Not a predicate");
    [Fact] void should_omit_the_rule_with_missing_identity() => _result.Source.ShouldNotContain("BeValidAccountNumber");
    [Fact] void should_not_emit_an_empty_validation_block() => _result.Source.ShouldNotContain("validate");
}
