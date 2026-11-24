// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_only_the_winner_owns_diagnostics : given.role_candidates
{
    given.selection_result _result = null!;
    void Because()
    {
        var earlier = Candidate("first:one", "firstId", 1, diagnostics: [Warning("LOSER_PAYLOAD")]);
        var later = Candidate("first:two", "secondId", 2, diagnostics: [Warning("WINNER_PAYLOAD")]);
        _result = given.selection_result.FromContributions(Inputs(
            earlier,
            later,
            Support(earlier, [new() { Method = Method, Diagnostics = [Warning("LOSER_SUPPORT")] }]),
            Support(later, [new() { Method = Method, Diagnostics = [Warning("WINNER_SUPPORT")] }])));
    }
    [Fact] void should_publish_only_winner_owned_payloads() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).Order(StringComparer.Ordinal).ShouldContainOnly("WINNER_PAYLOAD", "WINNER_SUPPORT");
    [Fact] void should_give_each_published_payload_core_lineage() => _result.Diagnostics.All(diagnostic => diagnostic.Lineage?.Producer == given.selection_result.Rule).ShouldBeTrue();
    [Fact] void should_record_selection_support_as_evidence() => _result.SelectedRoles.Single().Lineage!.Inputs.Select(id => id.Value).ShouldContain("first:two:support");
    [Fact] void should_keep_evidence_aligned_with_inputs() => _result.SelectedRoles.Single().Lineage!.Inputs.Length.ShouldEqual(_result.SelectedRoles.Single().Lineage!.Evidence.Length);
    [Fact] void should_keep_both_alternatives_as_provenance() => _result.Dispositions.Values.All(disposition => disposition == GenerationFactDisposition.ProvenanceOnly).ShouldBeTrue();
}
