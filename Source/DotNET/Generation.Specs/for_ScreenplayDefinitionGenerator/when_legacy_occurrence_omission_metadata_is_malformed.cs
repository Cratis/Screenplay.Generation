// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_legacy_occurrence_omission_metadata_is_malformed : for_GenerationFactDerivation.given.occurrence_omissions
{
    [Fact] void should_report_missing_definitions_without_emitting_payloads() => Check(candidate => candidate with { Definition = null! });
    [Fact] void should_report_unknown_policy_without_emitting_payloads() => Check(candidate => candidate with { Definition = candidate.Definition with { Policy = OccurrenceOmissionDiagnosticPolicy.Unknown } });
    [Fact] void should_report_unknown_preference_without_emitting_payloads() => Check(candidate => candidate with { Definition = candidate.Definition with { Preference = OmissionDiagnosticEvidencePreference.Unknown } });
    [Fact] void should_report_foreign_support_without_consuming_another_producers_facts() => Check(candidate => candidate with { Definition = candidate.Definition with { Diagnostic = candidate.Definition.Diagnostic with { Facts = [new() { Value = "good:observation" }] } } });
    [Fact] void should_report_missing_local_support_without_emitting_payloads() => Check(candidate => candidate with { Definition = candidate.Definition with { Diagnostic = candidate.Definition.Diagnostic with { Facts = [new() { Value = "bad:missing" }] } } });

    static void Check(Func<OccurrenceOmissionDiagnosticFact, OccurrenceOmissionDiagnosticFact> malformed)
    {
        var good = Candidate("good");
        var bad = Candidate("bad", OmissionDiagnosticEvidencePreference.IdentityConfiguration);
        var facts = new GenerationFact[] { good, OmissionObservation(good), malformed(bad), OmissionObservation(bad) };
        var result = new ScreenplayDefinitionGenerator().Generate(OmissionSnapshot(facts), new() { Domain = "Diagnostics" });
        result.Diagnostics.Count(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidOccurrenceOmissionDiagnostic).ShouldEqual(1);
        result.Diagnostics.Single(diagnostic => diagnostic.Code == "OMIT0044").Source.ShouldEqual(good.Evidence.Source);
        result.Diagnostics.Single(diagnostic => diagnostic.Code == "OMIT0044").Lineage.Evidence.Any(evidence => evidence.Adapter == bad.Evidence.Adapter).ShouldBeFalse();
    }
}
