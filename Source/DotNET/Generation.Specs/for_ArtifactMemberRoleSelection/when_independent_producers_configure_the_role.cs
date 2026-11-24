// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_independent_producers_configure_the_role : given.role_candidates
{
    void Because() => GenerateBoth(Candidate("first:earlier", "firstId", 1), Candidate("second:later", "secondId", 2, producer: Second));

    [Fact] void should_select_the_later_member() => Selected(Candidate("first:earlier", "firstId", 1), Candidate("second:later", "secondId", 2, producer: Second)).Single().Definition.Member.Name.ShouldEqual("secondId");
    [Fact] void should_preserve_all_output_when_reversed() => AdapterRunProjection(Reverse).ShouldEqual(AdapterRunProjection(Forward));
}
