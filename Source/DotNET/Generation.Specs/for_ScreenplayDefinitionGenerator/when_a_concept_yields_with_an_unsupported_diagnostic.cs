// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_concept_yields_with_an_unsupported_diagnostic : given.yielding_concepts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var candidate = Candidate("enum", true);
        var declaration = candidate.OfType<ArtifactDeclarationFact>().Single();
        var marker = declaration.Definition.NameConflictYield!;
        var unsupported = declaration with
        {
            Definition = declaration.Definition with
            {
                NameConflictYield = marker with { Diagnostic = marker.Diagnostic with { Outcome = GenerationDiagnosticOutcome.Unsupported } }
            }
        };
        _result = Generate([.. Command(), .. candidate.Where(fact => fact != declaration), unsupported, .. Candidate("vogen", false, true)]);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_normal_concept() => _result.Source.ShouldContain("concept Permissions : Int");
    [Fact] void should_drop_the_yielding_concept() => _result.Source.ShouldNotContain("concept Permissions : Enum");
    [Fact] void should_keep_the_unbound_command() => _result.Source.ShouldContain("permissions Int");
    [Fact] void should_not_bind_to_the_winning_name() => Member(_result).Type.Subject.ShouldBeNull();
    [Fact] void should_report_the_adapter_diagnostic_once() => _result.Diagnostics.Count(IsWarning).ShouldEqual(1);
    [Fact] void should_preserve_the_adapter_outcome() => _result.Diagnostics.Single(IsWarning).Outcome.ShouldEqual(GenerationDiagnosticOutcome.Unsupported);
    [Fact] void should_preserve_the_adapter_source() => _result.Diagnostics.Single(IsWarning).Source!.Path.ShouldEqual("enum.cs");
    [Fact] void should_omit_the_declaration_with_diagnostic() => _result.AdapterRun.Facts.Single(record => record.Fact.Id == Id("concept-enum")).Disposition.ShouldEqual(GenerationFactDisposition.OmittedWithDiagnostic);
}
