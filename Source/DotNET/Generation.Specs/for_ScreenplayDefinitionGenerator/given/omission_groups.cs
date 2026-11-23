// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class omission_groups : a_generator
{
    protected static readonly SubjectId Owner = new() { Value = "dotnet://Diagnostics/Input" };
    protected FactId Id(string value) => new() { Value = $"{Adapter.Id}:{value}" };
    protected ArtifactPlacementFact Member(string name) => new()
    {
        Id = Id(name), Subject = new() { Value = $"{Owner.Value}/{name}" }, Evidence = Exact(),
        Artifact = new() { Subject = new() { Value = $"{Owner.Value}/{name}" }, Kind = ArtifactKind.Command },
        Placement = new() { Module = "Diagnostics", Features = ["Inputs"], Slice = name, SliceKind = GenerationSliceKind.StateChange }
    };
    protected OmittedFactDiagnosticGroupFact Group(params GenerationFact[] covered) => new()
    {
        Id = Id("group"), Subject = Owner, Evidence = Exact(),
        Definition = new()
        {
            CoveredFacts = [.. covered.Select(fact => fact.Id)],
            Diagnostic = new() { Code = "OBS_GROUP", Severity = GenerationDiagnosticSeverity.Warning, Message = "Input members could not be transferred", Subject = Owner, Source = Exact().Source, Facts = [covered[0].Id] }
        }
    };
    protected Evidence Exact() => new() { Adapter = Adapter, Strength = EvidenceStrength.Exact, Source = new() { Path = "Inputs.cs", StartLine = 1, StartColumn = 1, EndLine = 1, EndColumn = 5 } };

    protected GeneratedScreenplayDefinition Generate(IEnumerable<GenerationFact> facts, params GenerationDiagnostic[] diagnostics) =>
        Generator.Generate(Snapshot(Completed(Adapter, [.. facts], diagnostics)), new() { Domain = "Diagnostics" });
}
