// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_ordinary_diagnostics_resemble_occurrence_candidates : for_GenerationFactDerivation.given.occurrence_omissions
{
    GeneratedScreenplayDefinition _result;
    GenerationDiagnostic[] _ordinary;
    void Because()
    {
        var preferred = Candidate("registration", OmissionDiagnosticEvidencePreference.IdentityConfiguration);
        var other = Candidate("other");
        _ordinary = [preferred.Definition.Diagnostic, other.Definition.Diagnostic];
        var contributions = Contributions(CandidateFacts(preferred, other)).Select(contribution => contribution with
        {
            Diagnostics = [_ordinary.Single(diagnostic => diagnostic.Facts[0].Value.StartsWith($"{contribution.Adapter.Id}:", StringComparison.Ordinal))]
        });
        _result = new ScreenplayDefinitionGenerator().Generate(contributions, new() { Domain = "Diagnostics" });
    }
    [Fact] void should_keep_both_ordinary_diagnostics_and_the_opted_in_representative() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == "OMIT0044").ShouldEqual(3);
    [Fact] void should_preserve_ordinary_payloads_without_lineage() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == "OMIT0044" && diagnostic.Lineage is null).Select(diagnostic => JsonSerializer.Serialize(diagnostic)).Order(StringComparer.Ordinal).ShouldEqual(_ordinary.Select(diagnostic => JsonSerializer.Serialize(diagnostic)).Order(StringComparer.Ordinal));
    [Fact] void should_use_cross_producer_evidence_only_for_explicit_candidates() => _result.Diagnostics.Single(diagnostic => diagnostic.Lineage is not null).Lineage.Inputs.Length.ShouldEqual(4);
}
