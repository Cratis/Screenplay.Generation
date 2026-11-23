// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// States that the observing adapter intentionally contributed no target for a member's authored type use.
/// </summary>
/// <remarks>
/// A declared target takes precedence over this fact. Otherwise the member retains its unbound type
/// and the diagnostic is reported once. The same adapter must contribute the identified type use.
/// </remarks>
public sealed record TypeUseTargetOmittedFact : GenerationFact
{
    /// <summary>Gets the member type use and the adapter's reason for omitting its target.</summary>
    public required TypeUseTargetOmittedDefinition Definition { get; init; }
}

/// <summary>
/// Identifies a member type use whose observed target was intentionally omitted.
/// </summary>
public sealed record TypeUseTargetOmittedDefinition
{
    /// <summary>Gets the exact member identity used by the adapter's type-use fact.</summary>
    public required ArtifactMemberKey Member { get; init; }

    /// <summary>Gets the adapter's diagnostic, including its severity and source location.</summary>
    public required GenerationDiagnostic Diagnostic { get; init; }
}
