// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_specific_failures_prevent_omission_coalescing : given.omission_groups
{
    [Fact] void should_preserve_conflicts() => Preserve(GenerationDiagnosticCodes.ConflictingGenerationFact, GenerationDiagnosticOutcome.Conflict);
    [Fact] void should_preserve_missing_target_failures() => Preserve(GenerationDiagnosticCodes.MissingTypeUseTarget, GenerationDiagnosticOutcome.Unsupported);
    [Fact] void should_preserve_type_shape_failures() => Preserve(GenerationDiagnosticCodes.UnsupportedTypeUseShape, GenerationDiagnosticOutcome.Unsupported);
    [Fact] void should_preserve_eligibility_failures() => Preserve(GenerationDiagnosticCodes.InvalidMethodFactEligibility, GenerationDiagnosticOutcome.Unsupported);
    [Fact] void should_preserve_scope_collisions() => Preserve(GenerationDiagnosticCodes.BackendScopeOutputCollision, GenerationDiagnosticOutcome.Conflict);
    [Fact] void should_preserve_specific_diagnostics_without_an_outcome() => Preserve("OBS_FAILURE", null);
    [Fact] void should_not_coalesce_a_group_that_covers_a_lowered_fact()
    {
        var command = new ArtifactFact
        {
            Id = Id("command"), Subject = Owner, Evidence = Exact(),
            Definition = new() { Key = new() { Subject = Owner, Kind = ArtifactKind.Command }, Name = "Execute", File = "Execute.cs" }
        };
        var member = Member("member");
        var placement = new ArtifactPlacementFact
        {
            Id = Id("command-placement"), Subject = Owner, Artifact = command.Definition.Key, Evidence = Exact(),
            Placement = new() { Module = "Diagnostics", Features = ["Inputs"], Slice = "Execute", SliceKind = GenerationSliceKind.StateChange }
        };
        var result = Generate([command, placement, member, Group(command, member)]);
        result.AdapterRun.Facts.Single(record => record.Fact.Id == command.Id).Disposition.ShouldEqual(GenerationFactDisposition.Lowered);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "OBS_GROUP").ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.OmittedGenerationFact && diagnostic.Facts.Contains(member.Id)).ShouldBeTrue();
    }

    void Preserve(string code, GenerationDiagnosticOutcome? outcome)
    {
        var first = Member("first");
        var second = Member("second");
        var specific = new GenerationDiagnostic { Code = code, Message = "Specific failure", Severity = GenerationDiagnosticSeverity.Warning, Outcome = outcome, Facts = [first.Id], Source = Exact().Source, Subject = Owner };
        var result = Generate([first, second, Group(first, second)], specific);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeTrue();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "OBS_GROUP").ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.OmittedGenerationFact && diagnostic.Facts.Contains(second.Id)).ShouldBeTrue();
    }
}
