// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_generating_from_a_snapshot_with_excluded_support : given.method_facts
{
    GeneratedScreenplayDefinition _forward;
    GeneratedScreenplayDefinition _reverse;

    void Because()
    {
        GenerationFact[] facts = [.. CompleteFacts(), Support(), Classification(MethodExecutionKind.FailureOnly)];
        var generator = new ScreenplayDefinitionGenerator();
        _forward = generator.Generate(Run(facts), new ScreenplayGenerationOptions { Domain = "Application" });
        _reverse = generator.Generate(Run([.. facts.Reverse()]), new ScreenplayGenerationOptions { Domain = "Application" });
    }

    [Fact] void should_keep_the_original_candidate() => _forward.AdapterRun.Facts.Single(record => record.Fact.Id == Usage().Id).Fact.ShouldEqual(Usage());
    [Fact] void should_explain_the_candidates_omission() => _forward.AdapterRun.Facts.Single(record => record.Fact.Id == Usage().Id).Disposition.ShouldEqual(GenerationFactDisposition.OmittedWithDiagnostic);
    [Fact] void should_retain_the_classification_as_provenance() => _forward.AdapterRun.Facts.Single(record => record.Fact.Id == Classification(MethodExecutionKind.FailureOnly).Id).Disposition.ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_not_rejoin_the_excluded_usage() => _forward.Graph.Relationships.ShouldBeEmpty();
    [Fact] void should_be_deterministic() => Projection(_forward.AdapterRun).ShouldEqual(Projection(_reverse.AdapterRun));
    [Fact] void should_print_the_same_source() => _forward.Source.ShouldEqual(_reverse.Source);
}
