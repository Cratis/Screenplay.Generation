// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Maps a selected exact source owner through host-owned fixed source snapshots.</summary>
/// <remarks>Implementations must not read files, rerun adapters, or consult mutable discovery state during resolution.</remarks>
public interface IArtifactSourcePlacementResolver
{
    /// <summary>Resolves one independently selected owner.</summary>
    /// <param name="request">The exact target, owner and semantic slice kind.</param>
    /// <returns>The completed placement or blocking source diagnostics.</returns>
    SourcePlacementResolutionResult Resolve(SourcePlacementResolutionRequest request);
}

/// <summary>Requests source mapping for one exact artifact.</summary>
public sealed record SourcePlacementResolutionRequest
{
    /// <summary>Gets the exact artifact being placed.</summary>
    public required ArtifactKey Artifact { get; init; }

    /// <summary>Gets the selected source owner, including role and scope.</summary>
    public required ArtifactKey SourceOwner { get; init; }

    /// <summary>Gets the independently established semantic slice kind.</summary>
    public required GenerationSliceKind SliceKind { get; init; }
}

/// <summary>Contains a fixed-snapshot mapping result.</summary>
public sealed record SourcePlacementResolutionResult
{
    /// <summary>Gets the exact requested artifact identity.</summary>
    public required ArtifactKey Artifact { get; init; }

    /// <summary>Gets the placement when source mapping succeeded.</summary>
    public ArtifactPlacement? Placement { get; init; }

    /// <summary>Gets located source mapping diagnostics.</summary>
    public IReadOnlyList<GenerationDiagnostic> Diagnostics { get; init; } = [];
}
