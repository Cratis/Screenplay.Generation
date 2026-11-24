// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Describes one ordered alternative for an exclusive role on an exact artifact occurrence.
/// </summary>
public sealed record ArtifactMemberRoleSelectionDefinition
{
    /// <summary>Gets the exact artifact occurrence owning the role.</summary>
    public required ArtifactKey Artifact { get; init; }

    /// <summary>Gets the role selected independently of other roles.</summary>
    public required ArtifactMemberRoleKind Role { get; init; }

    /// <summary>Gets the selected member, or null to explicitly select no member.</summary>
    public required ArtifactMemberKey? Member { get; init; }

    /// <summary>Gets the signed priority. Higher values win before observation order is considered.</summary>
    public required int Priority { get; init; }

    /// <summary>Gets the nonnegative semantic observation order. Higher values mean later observations.</summary>
    public required long ObservationOrder { get; init; }

    /// <summary>Gets producer-owned diagnostics published only when this eligible alternative wins.</summary>
    public IReadOnlyList<GenerationDiagnostic> Diagnostics { get; init; } = [];
}

/// <summary>
/// Contributes a source-neutral exclusive member-role alternative; it does not itself assert a role.
/// Method support can qualify its eligibility; no support means unconditional eligibility.
/// </summary>
public sealed record ArtifactMemberRoleSelectionFact : GenerationFact
{
    /// <summary>Gets the alternative's exact group, rank, and optional member.</summary>
    public required ArtifactMemberRoleSelectionDefinition Definition { get; init; }
}
