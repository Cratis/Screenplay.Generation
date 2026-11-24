// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_no_member_wins : given.role_candidates
{
    given.selection_result _result = null!;
    void Because() => _result = given.selection_result.FromContributions(Inputs(Candidate("first:fallback", "firstId", 99, 0, diagnostics: [Warning("LOSER")]), Candidate("second:none", null, 2, producer: Second, diagnostics: [Warning()])));
    [Fact] void should_not_assert_any_role() => _result.Roles.ShouldBeEmpty();
    [Fact] void should_publish_only_the_winner_warning() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly("OWNED");
}
