// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_freezing_backend_scope_ownership : for_GenerationResolver.given.backend_scopes
{
    AdapterContributionAdmissionResult _result;
    ArtifactFact _artifact;
    BackendScopeFact _scope;

    void Because()
    {
        _scope = Scope(First, "scope");
        _artifact = Artifact(Key(EventSubject, ArtifactKind.Event, First), "event", "CustomerRegistered");
        var facts = new List<GenerationFact> { _scope, _artifact };
        _result = Admit([.. facts]);
        facts.Clear();
    }

    [Fact] void should_admit_the_scope_capability_and_scoped_event() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_copy_scope_keys_on_occurrences() => ReferenceEquals(_result.Snapshot.Facts.OfType<ArtifactFact>().Single().Definition.Key.Scope, _artifact.Definition.Key.Scope).ShouldBeFalse();
    [Fact] void should_copy_nested_store_subjects_on_scope_declarations() => ReferenceEquals(_result.Snapshot.Facts.OfType<BackendScopeFact>().Single().Definition.Key.Store, _scope.Definition.Key.Store).ShouldBeFalse();
    [Fact] void should_retain_both_facts_after_input_list_mutation() => _result.Snapshot.Facts.Length.ShouldEqual(2);
}
