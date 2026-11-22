// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_scoped_events_have_separate_output_namespaces : for_GenerationResolver.given.backend_scopes
{
    GeneratedScreenplayDefinition _result;

    void Because()
    {
        var first = Key(EventSubject, ArtifactKind.Event, First);
        var second = first with { Scope = Second };
        var placement = Placement(second, "second-placement");
        _result = GenerateSnapshot(
            Scope(First, "first-scope"),
            Scope(Second, "second-scope"),
            Artifact(first, "first", "CustomerRegistered"),
            Artifact(second, "second", "CustomerRegistered"),
            Placement(first, "first-placement"),
            placement with { Placement = placement.Placement with { Slice = "OtherRegister" } });
    }

    [Fact] void should_verify_both_scope_erased_declarations_in_their_host_selected_namespaces() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_both_occurrences_partially_lowered() => _result.AdapterRun.Facts.Where(record => record.Fact is ArtifactFact).All(record => record.Disposition == GenerationFactDisposition.PartiallyLowered).ShouldBeTrue();
    [Fact] void should_not_report_an_output_collision() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.BackendScopeOutputCollision).ShouldBeFalse();
}
