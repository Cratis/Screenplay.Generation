// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_independent_observations_omit_one_occurrence : given.occurrence_omissions
{
    OccurrenceOmissionDiagnosticFact[] _candidates;
    GenerationDerivationSnapshot _result;
    GenerationDiagnostic _diagnostic;
    void Because()
    {
        _candidates = [Candidate("registration", OmissionDiagnosticEvidencePreference.ExplicitRegistration), Candidate("projection"), Candidate("compiled-query"), Candidate("api-touch")];
        _result = Derive(CandidateFacts(_candidates));
        _diagnostic = Omissions(_result).Single();
    }
    [Fact] void should_report_one_information_omission() => _diagnostic.Severity.ShouldEqual(GenerationDiagnosticSeverity.Information);
    [Fact] void should_prefer_the_registration_location() => _diagnostic.Source.ShouldEqual(_candidates[0].Definition.Diagnostic.Source);
    [Fact] void should_keep_the_selected_producer_local_facts() => _diagnostic.Facts.Select(id => id.Value).ShouldEqual(_candidates[0].Definition.Diagnostic.Facts.Select(id => id.Value));
    [Fact] void should_retain_every_candidate_and_local_observation() => _diagnostic.Lineage.Inputs.Length.ShouldEqual(8);
    [Fact] void should_retain_every_primary_location() => _candidates.All(candidate => _diagnostic.Lineage.Evidence.Any(evidence => evidence.Source == candidate.Definition.Diagnostic.Source)).ShouldBeTrue();
    [Fact] void should_not_create_semantic_facts() => _result.Rules.Single(rule => rule.Rule.Id == _diagnostic.Lineage.Producer.Id).Outputs.ShouldBeEmpty();
}
