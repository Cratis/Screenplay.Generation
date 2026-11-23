// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Generation;

static class MixedSliceFallbackStructure
{
    public static string Policy(MixedSlicePlacementFallbackDefinition? fallback) => fallback is null ? string.Empty : Structural.SemanticKey("mixed-slice-policy", fallback.Version.ToString(CultureInfo.InvariantCulture), ((int)fallback.Mode).ToString(CultureInfo.InvariantCulture), fallback.Slice ?? string.Empty);

    public static string WithPolicy(string legacy, MixedSlicePlacementFallbackDefinition? fallback) => fallback is null ? legacy : Structural.SemanticKey(legacy, Policy(fallback));

    public static string WithIdentity(string legacy, MixedSlicePlacementFallbackDefinition? fallback) => fallback is null ? legacy : Structural.SemanticKey(WithPolicy(legacy, fallback), fallback.Evidence is null ? "missing-evidence" : Structural.Evidence(fallback.Evidence));

    public static MixedSlicePlacementFallbackDefinition? Copy(MixedSlicePlacementFallbackDefinition? fallback) => fallback is null ? null : fallback with
    {
        Evidence = fallback.Evidence is { } evidence ? evidence with { Adapter = evidence.Adapter with { }, Source = evidence.Source is { } source ? source with { } : null } : new Evidence { Adapter = new() { Id = string.Empty, Version = string.Empty }, Strength = EvidenceStrength.Unknown }
    };

    public static bool Supported(MixedSlicePlacementFallbackDefinition? fallback) => fallback is null || (ValidPolicy(fallback) && ValidEvidence(fallback.Evidence));

    public static bool ForFact(GenerationFact fact) => fact switch
    {
        ArtifactPlacementFact placement => Supported(placement.MixedSliceFallback),
        ArtifactSourcePlacementCandidateFact candidate => Supported(candidate.Definition.MixedSliceFallback),
        _ => true
    };

    static bool ValidPolicy(MixedSlicePlacementFallbackDefinition fallback) =>
        fallback.Version == 1 && fallback.Mode == MixedSlicePlacementFallbackMode.ArtifactThenKind &&
        (fallback.Slice is null || (!string.IsNullOrWhiteSpace(fallback.Slice) && fallback.Slice == fallback.Slice.Trim() && !fallback.Slice.Any(character => char.IsControl(character) || character is '/' or '\\')));

    static bool ValidEvidence(Evidence? evidence) => evidence is not null && Enum.IsDefined(evidence.Strength) && evidence.Strength != EvidenceStrength.Unknown;
}
