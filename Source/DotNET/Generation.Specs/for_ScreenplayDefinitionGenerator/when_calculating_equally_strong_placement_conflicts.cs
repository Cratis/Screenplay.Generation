// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_calculating_equally_strong_placement_conflicts : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var facts = Event("AccountOpened", "Open");
        var first = (ArtifactPlacementFact)facts[1];
        var second = first with
        {
            Id = new FactId { Value = "placement:AccountOpened:other" },
            Placement = first.Placement with { Slice = "Other" }
        };
        _result = Generator.Generate(
            Snapshot(Completed(Adapter, [facts[0], first, second])),
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_classify_the_first_conflict_variant_independently() => Disposition("placement:AccountOpened").ShouldEqual(GenerationFactDisposition.Conflicted);
    [Fact] void should_classify_the_second_conflict_variant_independently() => Disposition("placement:AccountOpened:other").ShouldEqual(GenerationFactDisposition.Conflicted);
    [Fact] void should_associate_the_stable_conflict_diagnostic_with_both_variants() => _result.AdapterRun!.Facts.Where(record => record.Disposition == GenerationFactDisposition.Conflicted).All(record => record.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingPlacement)).ShouldBeTrue();
    [Fact] void should_omit_the_artifact_that_could_not_be_placed() => Disposition("event:AccountOpened").ShouldEqual(GenerationFactDisposition.OmittedWithDiagnostic);
    [Fact] void should_not_leave_any_fact_unknown() => _result.AdapterRun!.Facts.Any(record => record.Disposition == GenerationFactDisposition.Unknown).ShouldBeFalse();

    GenerationFactDisposition Disposition(string id) => _result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == id).Disposition;
}
