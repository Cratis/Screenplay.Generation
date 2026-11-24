// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_an_eligible_alternative_uses_ordinary_classification : given.role_candidates
{
    given.selection_result _result = null!;
    void Because()
    {
        var winner = Candidate("first:winner", "secondId", 2);
        var classification = Failure() with { Definition = new() { Method = Method, Kind = MethodExecutionKind.Ordinary } };
        _result = given.selection_result.FromContributions(Inputs(Candidate("first:previous", "firstId", 1), winner, Support(winner), classification));
    }
    [Fact] void should_select_the_supported_configuration() => (_result.SelectedRoles.Single().Fact is ArtifactMemberRoleFact).ShouldBeTrue();
    [Fact] void should_include_the_relevant_classification_in_lineage() => _result.SelectedRoles.Single().Lineage!.Inputs.Select(id => id.Value).ShouldContain("frontend:failure");
    [Fact] void should_select_the_exact_member() => ((ArtifactMemberRoleFact)_result.SelectedRoles.Single().Fact).Definition.Member.Name.ShouldEqual("secondId");
}
