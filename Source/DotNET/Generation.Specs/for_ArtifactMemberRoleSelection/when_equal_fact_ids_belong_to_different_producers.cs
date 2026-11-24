// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_equal_fact_ids_belong_to_different_producers : given.role_candidates
{
    given.selection_result _result = null!;
    void Because()
    {
        var rejected = Candidate("shared", "firstId", 1);
        var winner = Candidate("shared", "secondId", 2, producer: Second, diagnostics: [Warning("WINNER")]);
        _result = given.selection_result.FromContributions(Inputs(rejected, Support(rejected), Failure(), winner));
    }
    [Fact] void should_not_alias_the_other_producers_support() => _result.Roles.Single().Definition.Member.Name.ShouldEqual("secondId");
    [Fact] void should_keep_only_the_eligible_producer_in_role_lineage() => _result.SelectedRoles.Single().Lineage!.Evidence.Select(evidence => evidence.Adapter.Id).ShouldContainOnly("second");
    [Fact] void should_keep_the_winner_payload() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("WINNER");
}
