// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_diagnostic_prose_names_an_unrelated_fact : given.a_generator
{
    GeneratedScreenplayDefinition _proseOnly = null!;
    GeneratedScreenplayDefinition _typed = null!;

    void Because()
    {
        var facts = Event("AccountOpened", "Open");
        var artifact = (ArtifactFact)facts[0];
        var diagnostic = new GenerationDiagnostic
        {
            Code = "ADAPTER9001",
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = GenerationDiagnosticOutcome.Conflict,
            Message = $"Fact '{artifact.Id.Value}' appears in presentation prose only",
            Subject = artifact.Subject
        };
        var options = new ScreenplayGenerationOptions { Domain = "Banking" };
        _proseOnly = Generator.Generate(
            Snapshot(Completed(Adapter, facts, [diagnostic])),
            options);
        _typed = Generator.Generate(
            Snapshot(Completed(Adapter, facts, [diagnostic with
            {
                Message = "Completely different presentation text",
                Facts = [artifact.Id]
            }])),
            options);
    }

    [Fact] void should_not_change_disposition_from_presentation_prose() => Disposition(_proseOnly).ShouldEqual(GenerationFactDisposition.Lowered);
    [Fact] void should_use_the_typed_link_independently_of_message_text() => Disposition(_typed).ShouldEqual(GenerationFactDisposition.Conflicted);
    [Fact] void should_retain_the_typed_link_on_the_associated_diagnostic() => Record(_typed).Diagnostics.Single().Facts.Single().Value.ShouldEqual("event:AccountOpened");

    static GenerationFactDisposition Disposition(GeneratedScreenplayDefinition result) => Record(result).Disposition;

    static GenerationFactRecord Record(GeneratedScreenplayDefinition result) => result.AdapterRun!.Facts
        .Single(record => record.Fact.Id.Value == "event:AccountOpened");
}
