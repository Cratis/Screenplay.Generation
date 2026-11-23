// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Defines the closed compatibility projection profiles.</summary>
public enum TypeUseCompatibilityProfile
{
    /// <summary>No supported profile was selected.</summary>
    Unknown = -1,

    /// <summary>Consumes outer optionality and at most one collection, preserving the selected symbol.</summary>
    OneLayerCollection = 0
}

/// <summary>Describes a versioned printable view of an exact local type-use observation.</summary>
public sealed record TypeUseCompatibilityProjectionDefinition
{
    /// <summary>Gets the original, same-producer observation identifier.</summary>
    public required FactId TypeUse { get; init; }

    /// <summary>Gets the explicitly selected compatibility profile.</summary>
    public required TypeUseCompatibilityProfile Profile { get; init; }

    /// <summary>Gets the profile version. Only version 1 is supported.</summary>
    public required int Version { get; init; }

    /// <summary>Gets the printable type and its selected source identity.</summary>
    public required TypeUseDefinition Type { get; init; }

    /// <summary>Gets the index of the source node printed by this view.</summary>
    public required int SourceNodeIndex { get; init; }
}

/// <summary>Opts one exact local observation into an explicitly versioned compatibility projection.</summary>
public sealed record TypeUseCompatibilityProjectionFact : GenerationFact
{
    /// <summary>Gets the separately attributable printable view.</summary>
    public required TypeUseCompatibilityProjectionDefinition Definition { get; init; }
}
