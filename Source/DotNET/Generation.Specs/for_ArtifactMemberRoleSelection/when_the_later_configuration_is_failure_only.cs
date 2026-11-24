// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_the_later_configuration_is_failure_only : given.role_candidates
{
    given.selection_result _result = null!;
    void Because()
    {
        var later = Candidate("second:later", null, 2, producer: Second, diagnostics: [Warning()]);
        _result = given.selection_result.FromContributions(Inputs(Candidate("first:earlier", "firstId", 1), later, Support(later), Failure()));
    }

    [Fact] void should_keep_the_previous_configuration() => _result.Roles.Single().Definition.Member.Name.ShouldEqual("firstId");
    [Fact] void should_keep_the_classification_warning() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("FAILURE_ONLY");
    [Fact] void should_not_publish_the_ineligible_warning() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("OWNED");
}
