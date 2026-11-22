// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Identifies a normalized input member owned by an exact source declaration occurrence.</summary>
public sealed record DeclarationMemberKey
{
    /// <summary>Gets the exact declaration identity, including substitutions supplied by its observer.</summary>
    public required SubjectId Declaration { get; init; }

    /// <summary>Gets the exact occurrence, or null for the unscoped occurrence.</summary>
    public BackendScopeKey? Scope { get; init; }

    /// <summary>Gets the normalized Screenplay member name.</summary>
    public required string Name { get; init; }
}

/// <summary>Declares a source input member independently of any artifact role.</summary>
public sealed record DeclarationInputMemberDeclarationDefinition
{
    /// <summary>Gets the declaration-owned member.</summary>
    public required DeclarationMemberKey Member { get; init; }

    /// <summary>Gets its nonnegative semantic source order.</summary>
    public required int DeclarationOrder { get; init; }
}

/// <summary>Observes the complete substituted type of a declaration-owned input member.</summary>
public sealed record DeclarationInputMemberTypeUseDefinition
{
    /// <summary>Gets the declaration-owned member.</summary>
    public required DeclarationMemberKey Member { get; init; }

    /// <summary>Gets the complete source-neutral type use, including wrapper order and exact target occurrence.</summary>
    public required TypeUseDefinition Type { get; init; }
}

/// <summary>Opts an exact entry-point occurrence into declaration-input transfer.</summary>
public sealed record EntryPointInputTransferDefinition
{
    /// <summary>Gets the exact executable whose ordered usages supply input members.</summary>
    public required SubjectId Method { get; init; }

    /// <summary>Gets the destination query occurrence.</summary>
    public required ArtifactKey Artifact { get; init; }

    /// <summary>Gets the usage role. Only Reads is supported by the built-in rule.</summary>
    public required RelationshipKind Kind { get; init; }
}

/// <summary>Observes a declaration-owned input member and its semantic order.</summary>
public sealed record DeclarationInputMemberDeclarationFact : GenerationFact
{
    /// <summary>Gets the declaration facet.</summary>
    public required DeclarationInputMemberDeclarationDefinition Definition { get; init; }
}

/// <summary>Observes the full substituted type of a declaration-owned input member.</summary>
public sealed record DeclarationInputMemberTypeUseFact : GenerationFact
{
    /// <summary>Gets the type-use facet.</summary>
    public required DeclarationInputMemberTypeUseDefinition Definition { get; init; }
}

/// <summary>Explicitly requests entry-point input transfer without changing Reads semantics.</summary>
public sealed record EntryPointInputTransferFact : GenerationFact
{
    /// <summary>Gets the transfer intent.</summary>
    public required EntryPointInputTransferDefinition Definition { get; init; }
}
