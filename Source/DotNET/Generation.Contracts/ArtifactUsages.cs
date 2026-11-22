// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Maps an exact source declaration to the artifact represented by a library construct.
/// </summary>
public sealed record ArtifactUsageDeclarationDefinition
{
    /// <summary>Gets the exact method or type declaration identity, never its display name.</summary>
    public required SubjectId Declaration { get; init; }

    /// <summary>Gets the exact target artifact whose existence is proved independently.</summary>
    public required ArtifactKey Artifact { get; init; }
}

/// <summary>
/// Establishes a library declaration's artifact meaning independently of entry-point discovery.
/// </summary>
public sealed record ArtifactUsageDeclarationFact : GenerationFact
{
    /// <summary>Gets the mapping; the fact subject equals its declaration.</summary>
    public required ArtifactUsageDeclarationDefinition Definition { get; init; }
}

/// <summary>
/// Establishes proved execution of a library declaration within an exact method.
/// </summary>
public sealed record MethodArtifactUsageDefinition
{
    /// <summary>Gets the exact containing method, including project, overload, and generic identity.</summary>
    public required SubjectId Method { get; init; }

    /// <summary>Gets the exact executed declaration, not an incidental syntax mention.</summary>
    public required SubjectId Declaration { get; init; }

    /// <summary>Gets the proved relationship role. The built-in join supports Reads only.</summary>
    public required RelationshipKind Kind { get; init; }

    /// <summary>Gets the exact entry-point occurrence selector; null selects an unscoped occurrence.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BackendScopeKey? SourceScope { get; init; }

    /// <summary>Gets the exact declaration target occurrence selector, independently of the source.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BackendScopeKey? TargetScope { get; init; }
}

/// <summary>
/// Establishes a method's artifact usage without knowing another adapter's entry points.
/// </summary>
public sealed record MethodArtifactUsageFact : GenerationFact
{
    /// <summary>Gets the usage; the fact subject equals its method.</summary>
    public required MethodArtifactUsageDefinition Definition { get; init; }
}

/// <summary>
/// Maps an exact method to its independently discovered entry-point artifact.
/// </summary>
public sealed record EntryPointDefinition
{
    /// <summary>Gets the exact entry-point method identity.</summary>
    public required SubjectId Method { get; init; }

    /// <summary>Gets the exact source artifact whose existence is proved independently.</summary>
    public required ArtifactKey Artifact { get; init; }
}

/// <summary>
/// Establishes an entry point independently of library usage discovery.
/// </summary>
public sealed record EntryPointFact : GenerationFact
{
    /// <summary>Gets the entry-point mapping; the fact subject equals its method.</summary>
    public required EntryPointDefinition Definition { get; init; }
}
