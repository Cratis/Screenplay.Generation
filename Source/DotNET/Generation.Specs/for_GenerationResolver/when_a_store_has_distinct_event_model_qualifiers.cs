// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_a_store_has_distinct_event_model_qualifiers : given.backend_scopes
{
    ResolvedApplicationGraph _graph;

    void Because() => _graph = new GenerationResolver().Resolve([Contribution(Scope(First with { EventModelName = "North|South:😀" }, "north"), Scope(First with { EventModelName = "North|South:😀x" }, "south"), Scope(First with { EventModelName = null }, "none"))]);

    [Fact] void should_keep_nullable_and_case_sensitive_qualifiers_in_identity() => _graph.BackendScopes.Count.ShouldEqual(3);
    [Fact] void should_not_treat_one_store_with_distinct_models_as_a_conflict() => _graph.Diagnostics.ShouldBeEmpty();
}
