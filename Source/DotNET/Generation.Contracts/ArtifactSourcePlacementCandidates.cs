// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Screenplay.Generation;

/// <summary>Identifies independently established source ownership.</summary>
public enum ArtifactSourcePlacementReason
{
    /// <summary>No supported ownership reason.</summary>
    Unknown = -1,

    /// <summary>The artifact owns its source.</summary>
    Self = 0,

    /// <summary>An independently proven command produces the event.</summary>
    ProducingCommand = 1
}

/// <summary>Defines supported source-owner selection modes.</summary>
public enum ArtifactSourcePlacementSelectionMode
{
    /// <summary>No supported selection mode.</summary>
    Unknown = -1,

    /// <summary>Rank evidence before collapsing distinct event producers.</summary>
    StrongestEvidenceThenEventProduction = 0
}

/// <summary>Opts an exact artifact into versioned source-owner selection.</summary>
public sealed record ArtifactSourcePlacementSelectionPolicy
{
    /// <summary>Gets the supported policy version.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Gets the selection mode.</summary>
    public ArtifactSourcePlacementSelectionMode Mode { get; init; } = ArtifactSourcePlacementSelectionMode.StrongestEvidenceThenEventProduction;
}

/// <summary>Contributes one independent source-owner observation, not a completed placement.</summary>
public sealed record ArtifactSourcePlacementCandidateDefinition
{
    /// <summary>Gets the exact target role and scope.</summary>
    public required ArtifactKey Artifact { get; init; }

    /// <summary>Gets the exact proposed source owner.</summary>
    public required ArtifactKey Owner { get; init; }

    /// <summary>Gets the independently established slice kind.</summary>
    public required GenerationSliceKind SliceKind { get; init; }

    /// <summary>Gets the independently established ownership reason.</summary>
    public required ArtifactSourcePlacementReason Reason { get; init; }

    /// <summary>Gets the explicit versioned selection policy.</summary>
    public required ArtifactSourcePlacementSelectionPolicy Policy { get; init; }

    /// <summary>Gets optional attributable mixed-kind splitting metadata to propagate to the derived placement.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MixedSlicePlacementFallbackDefinition? MixedSliceFallback { get; init; }
}

/// <summary>Asserts a source-owner candidate using attributable adapter evidence.</summary>
public sealed record ArtifactSourcePlacementCandidateFact : GenerationFact
{
    /// <summary>Gets the exact candidate definition.</summary>
    public required ArtifactSourcePlacementCandidateDefinition Definition { get; init; }
}
