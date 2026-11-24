// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_ranking_eligible_alternatives : given.role_candidates
{
    void Because() => GenerateBoth(Candidate("first:one", "firstId", 1), Candidate("first:two", "secondId", 2), Candidate("first:three", "thirdId", 3));
    [Fact] void should_select_only_the_third_same_producer_observation() => Selected(Candidate("first:one", "firstId", 1), Candidate("first:two", "secondId", 2), Candidate("first:three", "thirdId", 3)).Single().Definition.Member.Name.ShouldEqual("thirdId");
    [Fact] void should_prefer_priority_to_chronology() => Selected(Candidate("first:fallback", "firstId", long.MaxValue, -1), Candidate("second:explicit", "secondId", 0, 0, Second)).Single().Definition.Member.Name.ShouldEqual("secondId");
    [Fact] void should_preserve_full_output_under_permutation() => AdapterRunProjection(Reverse).ShouldEqual(AdapterRunProjection(Forward));
    [Fact] void should_lower_only_the_selected_member() => Forward.Source.ShouldContain("thirdId Uuid identifier");
    [Fact] void should_not_lower_the_previous_member() => Forward.Source.ShouldNotContain("secondId Uuid identifier");
    [Fact] void should_not_create_duplicate_property_variants() => Forward.Graph.Artifacts.Single(artifact => artifact.Key == Artifact).Variants.Single().Definition.Properties.Count.ShouldEqual(3);
}
