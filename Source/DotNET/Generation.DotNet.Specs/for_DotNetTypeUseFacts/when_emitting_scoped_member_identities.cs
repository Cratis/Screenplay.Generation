// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeUseFacts;

public class when_emitting_scoped_member_identities : given.an_inherited_surface
{
    IReadOnlyList<GenerationFact> _first;
    IReadOnlyList<GenerationFact> _second;
    IReadOnlyList<GenerationFact> _legacy;
    IReadOnlyList<GenerationFact> _explicitNull;

    void Because()
    {
        var scope = new BackendScopeKey { Store = new SubjectId { Value = "dotnet://Application/Stores.Registration" }, EventModelName = "North|South:😀" };
        _first = DotNetTypeUseFacts.Emit(Type, Artifact with { Scope = scope }, Context, Evidence);
        _second = DotNetTypeUseFacts.Emit(Type, Artifact with { Scope = scope with { EventModelName = "North|South:😀x" } }, Context, Evidence);
        _legacy = DotNetTypeUseFacts.Emit(Type, Artifact, Context, Evidence);
        _explicitNull = DotNetTypeUseFacts.Emit(Type, Artifact with { Scope = null }, Context, Evidence);
    }

    [Fact] void should_distinguish_scopes_without_changing_the_clr_subject() => _first.Select(fact => fact.Id).Intersect(_second.Select(fact => fact.Id)).ShouldBeEmpty();
    [Fact] void should_preserve_legacy_null_scope_ids() => _explicitNull.Select(fact => fact.Id).ShouldEqual(_legacy.Select(fact => fact.Id));
    [Fact] void should_keep_all_subjects_unchanged() => _first.All(fact => fact.Subject == Artifact.Subject).ShouldBeTrue();
}
