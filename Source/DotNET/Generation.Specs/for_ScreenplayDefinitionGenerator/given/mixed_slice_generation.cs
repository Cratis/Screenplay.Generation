// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class mixed_slice_generation : a_lowering_fixture
{
    protected static readonly AdapterIdentity OtherAdapter = new() { Id = "read-models", Version = "1.0.0" };
    protected static readonly ScreenplayGenerationOptions Options = new() { Domain = "Banking", SourcePlacementResolver = new SharedPlacementResolver() };
    protected GenerationFact[] Write = [];
    protected GenerationFact[] Read = [];

    void Establish()
    {
        Write = Annotate(Artifact(ArtifactKind.Command, "Write", "Shared", GenerationSliceKind.StateChange, 1), Adapter);
        Read = Annotate(Artifact(ArtifactKind.ReadModel, "View", "Shared", GenerationSliceKind.StateView, 2), OtherAdapter);
    }

    protected GeneratedScreenplayDefinition GenerateRun(GenerationFact[] write, GenerationFact[] read, bool reverse = false)
    {
        var adapters = new[] { Completed(Adapter, write), Completed(OtherAdapter, read) };
        return Generator.Generate(Snapshot(reverse ? [.. adapters.Reverse()] : adapters), Options);
    }

    protected static GenerationFact[] AsCandidates(GenerationFact[] facts) => [.. facts.Select(fact => fact is ArtifactPlacementFact placement ? Candidate(placement) : fact)];

    protected static AdapterRunSnapshot RunOf(GeneratedScreenplayDefinition result) => result.AdapterRun ?? throw new InvalidOperationException("Expected a generation snapshot");

    protected static GenerationFactLineage LineageOf(GenerationFactRecord record) => record.Lineage ?? throw new InvalidOperationException("Expected derived fact lineage");

    protected static GenerationFactRecord[] Normalized(GeneratedScreenplayDefinition result) => [.. RunOf(result).Derivation.Facts.Where(record => record.Lineage?.Producer.Id == "cratis.screenplay.mixed-slice-placement")];

    static GenerationFact[] Annotate(GenerationFact[] facts, AdapterIdentity adapter) => [.. facts.Select(fact => fact is ArtifactPlacementFact placement
        ? placement with { Evidence = placement.Evidence with { Adapter = adapter }, MixedSliceFallback = new() { Slice = placement.Artifact.Kind == ArtifactKind.Command ? "Write" : "View", Evidence = placement.Evidence with { Adapter = adapter } } }
        : fact with { Evidence = fact.Evidence with { Adapter = adapter } })];

    static ArtifactSourcePlacementCandidateFact Candidate(ArtifactPlacementFact placement) => new()
    {
        Id = placement.Id,
        Subject = placement.Subject,
        Evidence = placement.Evidence,
        Definition = new()
        {
            Artifact = placement.Artifact,
            Owner = placement.Artifact,
            Reason = ArtifactSourcePlacementReason.Self,
            SliceKind = placement.Placement.SliceKind,
            Policy = new(),
            MixedSliceFallback = placement.MixedSliceFallback
        }
    };

    sealed class SharedPlacementResolver : IArtifactSourcePlacementResolver
    {
        public SourcePlacementResolutionResult Resolve(SourcePlacementResolutionRequest request) => new()
        {
            Artifact = request.Artifact,
            Placement = new() { Module = "Accounts", Features = ["Opening"], Slice = "Shared", SliceKind = request.SliceKind }
        };
    }
}
