// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_ineligible_declaration_precedes_the_occurrence : given.artifact_diagnostic_sources
{
    GeneratedScreenplayDefinition _result;
    ArtifactDeclarationFact _excluded;

    void Because()
    {
        _excluded = Declaration("excluded", Document, 4);
        var method = SubjectOf("FailureOnly");
        var support = new MethodFactSupportFact
        {
            Id = new FactId { Value = "support" },
            Subject = Document.Subject,
            Evidence = _excluded.Evidence,
            Definition = new MethodFactSupportDefinition
            {
                Candidate = _excluded.Id,
                Alternatives = [new MethodFactSupportAlternative { Method = method }]
            }
        };
        var classification = new MethodExecutionClassificationFact
        {
            Id = new FactId { Value = "classification" },
            Subject = method,
            Evidence = _excluded.Evidence,
            Definition = new MethodExecutionClassificationDefinition
            {
                Method = method,
                Kind = MethodExecutionKind.FailureOnly,
                OmissionReason = new GenerationDiagnostic
                {
                    Code = "FAILURE_ONLY",
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Message = "The executable is failure-only",
                    Source = _excluded.Evidence.Source
                }
            }
        };
        _result = GenerateSnapshot([_excluded, support, classification, Declaration("eligible", Document), .. Members("document", Document)]);
    }

    [Fact] void should_only_select_an_eligible_declaration_source() => Omissions(_result).Single().Source.ShouldEqual(At(5, 83, 105).Source);
    [Fact] void should_not_restore_ineligible_evidence_to_the_graph() => _result.Graph.Artifacts.Single().Variants.Single().Evidence.Contains(_excluded.Evidence).ShouldBeFalse();
    [Fact] void should_preserve_the_method_eligibility_omission() => _result.AdapterRun.Facts.Single(record => record.Fact.Id == _excluded.Id).Disposition.ShouldEqual(GenerationFactDisposition.OmittedWithDiagnostic);
}
