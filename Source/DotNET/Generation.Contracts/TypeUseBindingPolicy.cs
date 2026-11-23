// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Defines the producer's binding policy for an exact observed type use.</summary>
public enum TypeUseBindingPolicyKind
{
    /// <summary>The policy is unknown.</summary>
    Unknown = 0,

    /// <summary>Bind only a surviving concept, otherwise preserve the printable observed type.</summary>
    ConceptOnlyWithUnboundFallback = 1
}

/// <summary>Opts one producer's exact type observation into concept-only binding.</summary>
public sealed record TypeUseBindingPolicyDefinition
{
    /// <summary>Gets the local artifact-member or declaration-input type-use fact.</summary>
    public required FactId TypeUse { get; init; }

    /// <summary>Gets the binding policy.</summary>
    public required TypeUseBindingPolicyKind Policy { get; init; }
}

/// <summary>A producer-owned binding policy for one observation, not a target assertion.</summary>
public sealed record TypeUseBindingPolicyFact : GenerationFact
{
    /// <summary>Gets the policy.</summary>
    public required TypeUseBindingPolicyDefinition Definition { get; init; }
}
