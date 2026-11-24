// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_artifact_occurrences_share_a_subject : given.artifact_diagnostic_sources
{
    GeneratedScreenplayDefinition _result;
    GeneratedScreenplayDefinition _reversed;

    void Because()
    {
        var first = new BackendScopeKey { Store = SubjectOf("FirstStore") };
        var second = new BackendScopeKey { Store = SubjectOf("SecondStore") };
        var firstDocument = Document with { Scope = first };
        var secondDocument = Document with { Scope = second };
        var handler = Document with { Kind = ArtifactKind.Handler };
        GenerationFact[] facts =
        [
            Scope(first, "first"), Scope(second, "second"),
            Declaration("first", firstDocument), .. Members("first", firstDocument),
            Declaration("second", secondDocument, 8), .. Members("second", secondDocument),
            Declaration("handler", handler, 11), .. Members("handler", handler)
        ];
        _result = Generate(Contribution(facts));
        _reversed = Generate(Contribution([.. facts.Reverse()]));
    }

    [Fact] void should_keep_the_declaration_source_for_each_scope_and_role() => Omissions(_result).Select(diagnostic => diagnostic.Source.StartLine).Order().ShouldEqual([5, 8, 11]);
    [Fact] void should_keep_each_occurrence_separate() => _result.Graph.Artifacts.Count.ShouldEqual(3);
    [Fact] void should_not_conflict_the_occurrences() => _result.Graph.Artifacts.Any(artifact => artifact.IsConflicted).ShouldBeFalse();
    [Fact] void should_keep_source_selection_order_independent() => Omissions(_reversed).Select(diagnostic => diagnostic.Source).ShouldEqual(Omissions(_result).Select(diagnostic => diagnostic.Source));

    BackendScopeFact Scope(BackendScopeKey scope, string id) => new()
    {
        Id = new FactId { Value = $"scope:{id}" },
        Subject = scope.Store,
        Evidence = At(1, 1, 10),
        Definition = new BackendScopeDefinition { Key = scope, Name = id }
    };
}
