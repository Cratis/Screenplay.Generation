// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Generation;

static class SourcePlacementCandidateStructure
{
    public static string Definition(ArtifactSourcePlacementCandidateDefinition definition) => MixedSliceFallbackStructure.WithIdentity(Key(Copy(definition, key => key)), definition?.MixedSliceFallback);

    public static bool Supported(ArtifactSourcePlacementCandidateDefinition definition) =>
        definition?.Artifact is not null && definition.Owner is not null && definition.Policy is not null && Valid(definition);

    public static ArtifactSourcePlacementCandidateDefinition Copy(ArtifactSourcePlacementCandidateDefinition? definition, Func<ArtifactKey, ArtifactKey> copyKey) => new()
    {
        Artifact = copyKey(definition?.Artifact ?? MissingKey()),
        Owner = copyKey(definition?.Owner ?? MissingKey()),
        SliceKind = definition?.SliceKind ?? GenerationSliceKind.Unknown,
        Reason = definition?.Reason ?? ArtifactSourcePlacementReason.Unknown,
        Policy = definition?.Policy is { } policy ? policy with { } : new() { Version = -1 },
        MixedSliceFallback = MixedSliceFallbackStructure.Copy(definition?.MixedSliceFallback)
    };

    static ArtifactKey MissingKey() => new() { Subject = new SubjectId { Value = string.Empty }, Kind = ArtifactKind.Unknown };

    static string Key(ArtifactSourcePlacementCandidateDefinition definition) => Structural.SemanticKey(
        "source-owner",
        Structural.ArtifactKey(definition.Artifact),
        Structural.ArtifactKey(definition.Owner),
        ((int)definition.SliceKind).ToString(CultureInfo.InvariantCulture),
        ((int)definition.Reason).ToString(CultureInfo.InvariantCulture),
        definition.Policy.Version.ToString(CultureInfo.InvariantCulture),
        ((int)definition.Policy.Mode).ToString(CultureInfo.InvariantCulture));

    static bool Valid(ArtifactSourcePlacementCandidateDefinition definition) =>
        Enum.IsDefined(definition.Artifact.Kind) && definition.Artifact.Kind != ArtifactKind.Unknown &&
        Enum.IsDefined(definition.Owner.Kind) && definition.Owner.Kind != ArtifactKind.Unknown &&
        Enum.IsDefined(definition.SliceKind) && definition.SliceKind != GenerationSliceKind.Unknown &&
        Enum.IsDefined(definition.Reason) && definition.Reason != ArtifactSourcePlacementReason.Unknown &&
        definition.Policy.Version == 1 && definition.Policy.Mode == ArtifactSourcePlacementSelectionMode.StrongestEvidenceThenEventProduction;
}
