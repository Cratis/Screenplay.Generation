// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_one_scoped_event : for_GenerationResolver.given.backend_scopes
{
    GeneratedScreenplayDefinition _result;

    void Because()
    {
        var key = Key(EventSubject, ArtifactKind.Event, First);
        _result = GenerateSnapshot(Scope(First, "scope"), Artifact(key, "event", "CustomerRegistered"), Placement(key, "placement"));
    }

    [Fact] void should_emit_only_existing_structural_syntax() => _result.Source.ShouldContain("event CustomerRegistered");
    [Fact] void should_verify_the_current_screenplay_document() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_invent_backend_syntax() => _result.Source.ShouldNotContain("event-model");
    [Fact] void should_report_located_scope_semantic_loss() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.BackendScopeNotSupported).Source.Path.ShouldEqual("Stores/Registration.cs");
    [Fact] void should_mark_the_event_as_partially_lowered() => Record("event").Disposition.ShouldEqual(GenerationFactDisposition.PartiallyLowered);
    [Fact] void should_attach_the_unsupported_diagnostic_to_the_partial_record() => Record("event").Diagnostics.Single().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Unsupported);
    [Fact] void should_keep_the_scope_declaration_as_provenance() => Record("scope").Disposition.ShouldEqual(GenerationFactDisposition.ProvenanceOnly);

    GenerationFactRecord Record(string id) => _result.AdapterRun.Facts.Single(record => record.Fact.Id == Id(id));
}
