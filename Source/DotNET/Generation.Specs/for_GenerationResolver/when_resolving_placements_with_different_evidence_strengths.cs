// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_placements_with_different_evidence_strengths : given.facts
{
    ResolvedArtifactPlacement _placement = null!;
    ResolvedApplicationGraph _graph = null!;

    void Because()
    {
        var artifact = EventDefinition().Key;
        _graph = new GenerationResolver().Resolve(
        [
            Contribution(FirstAdapter, Placement("heuristic", artifact, "Events", EvidenceStrength.Heuristic, FirstAdapter)),
            Contribution(SecondAdapter, Placement("exact", artifact, "Open", EvidenceStrength.Exact, SecondAdapter))
        ]);
        _placement = _graph.Placements.Single();
    }

    [Fact] void should_retain_both_variants() => _placement.Variants.Count.ShouldEqual(2);
    [Fact] void should_select_only_the_strongest_variant() => _placement.EffectiveVariants.Count.ShouldEqual(1);
    [Fact] void should_select_the_exact_placement() => _placement.EffectiveVariants.Single().Placement.Slice.ShouldEqual("Open");
    [Fact] void should_not_report_a_conflict() => _graph.Diagnostics.ShouldBeEmpty();

    static ArtifactPlacementFact Placement(
        string id,
        ArtifactKey artifact,
        string slice,
        EvidenceStrength strength,
        AdapterIdentity adapter) => new()
    {
        Id = new FactId { Value = id },
        Subject = artifact.Subject,
        Artifact = artifact,
        Placement = new ArtifactPlacement
        {
            Module = "Accounts",
            Features = ["Accounts"],
            Slice = slice,
            SliceKind = GenerationSliceKind.StateChange
        },
        Evidence = new Evidence { Adapter = adapter, Strength = strength }
    };
}
