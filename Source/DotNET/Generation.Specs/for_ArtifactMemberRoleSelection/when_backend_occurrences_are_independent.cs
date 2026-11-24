// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_backend_occurrences_are_independent : given.role_candidates
{
    given.selection_result _result = null!;
    protected static readonly BackendScopeKey North = new() { Store = new() { Value = "source://store/north" }, EventModelName = "Orders" };
    protected static readonly BackendScopeKey South = new() { Store = new() { Value = "source://store/south" }, EventModelName = "Orders" };
    protected static readonly BackendScopeKey OtherModel = North with { EventModelName = "Returns" };
    void Because() => _result = given.selection_result.FromContributions(Inputs(
        ScopeFact(North),
        ScopeFact(South),
        ScopeFact(OtherModel),
        Candidate("first:unscoped", "firstId", 0),
        Scoped(Candidate("first:north", "secondId", 0), North),
        Scoped(Candidate("first:south", "thirdId", 0), South),
        Scoped(Candidate("first:other-model", null, 0, diagnostics: [Warning()]), OtherModel)));
    [Fact] void should_select_three_separate_members() => _result.SelectedRoles.Count.ShouldEqual(3);
    [Fact] void should_keep_unscoped_distinct_from_scoped() => _result.Roles.Single(role => role.Definition.Member.Artifact.Scope is null).Definition.Member.Name.ShouldEqual("firstId");
    [Fact] void should_keep_stores_distinct() => _result.Roles.Single(role => role.Definition.Member.Artifact.Scope == South).Definition.Member.Name.ShouldEqual("thirdId");
    [Fact] void should_not_leak_a_no_member_selection_between_models() => _result.Roles.Single(role => role.Definition.Member.Artifact.Scope == North).Definition.Member.Name.ShouldEqual("secondId");
    [Fact] void should_use_scope_proofs_in_lineage() => _result.SelectedRoles.Where(record => ((ArtifactMemberRoleFact)record.Fact).Definition.Member.Artifact.Scope is not null).All(record => record.Lineage!.Inputs.Any(id => id.Value.StartsWith("first:scope:", StringComparison.Ordinal))).ShouldBeTrue();
}
