// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Records an adapter's intentional omission of a declaration input member's observed type target.
/// </summary>
public sealed record DeclarationInputMemberTargetOmittedDefinition
{
    /// <summary>Gets the exact declaration-owned member whose type target was omitted.</summary>
    public required DeclarationMemberKey Member { get; init; }

    /// <summary>Gets the producer-owned warning explaining the omitted target.</summary>
    public required GenerationDiagnostic Diagnostic { get; init; }
}

/// <summary>
/// Preserves an authored input type when its observed target is intentionally absent.
/// </summary>
public sealed record DeclarationInputMemberTargetOmittedFact : GenerationFact
{
    /// <summary>Gets the exact declaration member and its omission warning.</summary>
    public required DeclarationInputMemberTargetOmittedDefinition Definition { get; init; }
}
