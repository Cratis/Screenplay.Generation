// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Defines the fallback when a producer's concept-only type use has no concept target.</summary>
public enum TypeUseBindingFallback
{
    /// <summary>The fallback is unknown.</summary>
    Unknown = -1,

    /// <summary>Keep the observed member type without an artifact target.</summary>
    PreserveObservedTypeWithoutTarget = 0
}

/// <summary>Opts one producer's exact member type use into concept-only binding.</summary>
public sealed record TypeUseBindingPolicyDefinition
{
    /// <summary>Gets the member whose type use is constrained.</summary>
    public required ArtifactMemberKey Member { get; init; }

    /// <summary>Gets the allowed target kind. Only <see cref="ArtifactKind.Concept"/> is supported.</summary>
    public required ArtifactKind AllowedTargetKind { get; init; }

    /// <summary>Gets the fallback for a missing concept target.</summary>
    public required TypeUseBindingFallback Fallback { get; init; }

    /// <summary>Gets the producer-owned warning template, emitted only when fallback is needed.</summary>
    public required GenerationDiagnostic Diagnostic { get; init; }
}

/// <summary>A producer-owned binding policy for an exact member type use, not a target assertion.</summary>
public sealed record TypeUseBindingPolicyFact : GenerationFact
{
    /// <summary>Gets the policy.</summary>
    public required TypeUseBindingPolicyDefinition Definition { get; init; }
}
