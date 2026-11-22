// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_backend_occurrences_would_collapse_in_screenplay : for_GenerationResolver.given.backend_scopes
{
    GeneratedScreenplayDefinition _result;

    void Because()
    {
        var first = Key(EventSubject, ArtifactKind.Event, First);
        var second = first with { Scope = Second };
        var unrelated = Key(CommandSubject, ArtifactKind.Command, null);
        _result = GenerateSnapshot(Scope(First, "first-scope"), Scope(Second, "second-scope"), Artifact(first, "first", "CustomerRegistered"), Artifact(second, "second", "CustomerRegistered"), Placement(first, "first-placement"), Placement(second, "second-placement"), Artifact(unrelated, "unrelated", "Register"), Placement(unrelated, "unrelated-placement"));
    }

    [Fact] void should_retain_both_graph_occurrences() => _result.Graph.Artifacts.Count(artifact => artifact.Key.Subject == EventSubject).ShouldEqual(2);
    [Fact] void should_not_merge_rename_or_choose_an_event_occurrence() => _result.Source.ShouldNotContain("event CustomerRegistered");
    [Fact] void should_still_emit_unrelated_unscoped_artifacts() => _result.Source.ShouldContain("command Register");
    [Fact] void should_report_the_collision_as_an_error() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_the_scope_collision() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.BackendScopeOutputCollision && diagnostic.Severity == GenerationDiagnosticSeverity.Error && diagnostic.Source is not null).ShouldBeTrue();
    [Fact] void should_classify_both_omitted_event_facts() => _result.AdapterRun.Facts.Where(record => record.Fact is ArtifactFact fact && fact.Definition.Key.Subject == EventSubject).All(record => record.Disposition == GenerationFactDisposition.OmittedWithDiagnostic).ShouldBeTrue();
}
