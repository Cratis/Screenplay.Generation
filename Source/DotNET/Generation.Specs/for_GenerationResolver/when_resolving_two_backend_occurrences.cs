// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_two_backend_occurrences : given.backend_scopes
{
    ResolvedApplicationGraph _graph;
    ResolvedApplicationGraph _reversed;

    void Because()
    {
        var first = Key(EventSubject, ArtifactKind.Event, First);
        var second = first with { Scope = Second };
        var command = Key(CommandSubject, ArtifactKind.Command, null);
        GenerationFact[] facts = [Scope(First, "first-scope"), Scope(Second, "second-scope"), Artifact(first, "first", "CustomerRegistered"), Artifact(second, "second", "CustomerRegistered"), Placement(first, "first-placement"), Placement(second, "second-placement"), Reads(command, first, "first-read"), Reads(command, second, "second-read")];
        _graph = new GenerationResolver().Resolve([Contribution(facts)]);
        _reversed = new GenerationResolver().Resolve([Contribution([.. facts.Reverse()])]);
    }

    [Fact] void should_keep_exact_store_registrations_separate_despite_equal_display_and_event_model_names() => _graph.BackendScopes.Count.ShouldEqual(2);
    [Fact] void should_keep_the_shared_clr_event_subject_unchanged() => _graph.Artifacts.Select(artifact => artifact.Key.Subject).Distinct().Single().ShouldEqual(EventSubject);
    [Fact] void should_resolve_two_non_conflicted_occurrences() => _graph.Artifacts.Count(artifact => !artifact.IsConflicted).ShouldEqual(2);
    [Fact] void should_keep_scope_on_placements_and_relationship_endpoints() => _graph.Placements.Count.ShouldEqual(_graph.Relationships.Count);
    [Fact] void should_ignore_fact_order() => _reversed.Artifacts.Select(artifact => artifact.Key).ShouldEqual(_graph.Artifacts.Select(artifact => artifact.Key));
    [Fact] void should_not_conflict_across_distinct_scopes() => _graph.Diagnostics.ShouldBeEmpty();
}
