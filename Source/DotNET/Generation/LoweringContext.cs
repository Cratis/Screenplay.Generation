// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Represents an artifact together with the placement and source it was lowered from.
/// </summary>
/// <param name="Definition">The artifact definition.</param>
/// <param name="Placement">The effective placement of the artifact.</param>
/// <param name="Source">The first source location of the placement evidence.</param>
internal sealed record PlacedArtifact(
    ArtifactDefinition Definition,
    ArtifactPlacement Placement,
    SourceRange? Source);

/// <summary>
/// Provides the resolved relationships, artifact names and concept names the lowering steps share.
/// </summary>
/// <param name="graph">The resolved application graph.</param>
/// <param name="conceptNames">The emitted concept names by source subject.</param>
/// <param name="sink">The sink collecting diagnostics and coverage.</param>
internal sealed class LoweringContext(
    ResolvedApplicationGraph graph,
    IReadOnlyDictionary<string, string> conceptNames,
    LoweringSink sink)
{
    readonly IReadOnlyList<RelationshipDefinition> _relationships =
    [
        .. graph.Relationships
            .Where(_ => !_.IsConflicted &&
                        _.Key.Kind != RelationshipKind.Unknown &&
                        Enum.IsDefined(_.Key.Kind))
            .Select(_ => _.Definitions.Single())
    ];
    readonly IReadOnlyList<ArtifactDefinition> _artifacts =
    [
        .. graph.Artifacts
            .Where(_ => !_.IsConflicted)
            .Select(_ => _.Variants.Single().Definition)
    ];

    public LoweringSink Sink { get; } = sink;

    public RelationshipDefinition[] RelationshipsFrom(SubjectId source, RelationshipKind kind) =>
    [
        .. _relationships
            .Where(_ => _.Key.Source == source && _.Key.Kind == kind)
            .OrderBy(Canonical.Relationship, StringComparer.Ordinal)
    ];

    public string? ArtifactName(SubjectId subject, ArtifactKind kind) =>
        _artifacts.FirstOrDefault(_ => _.Key.Subject == subject && _.Key.Kind == kind)?.Name;

    public string TypeName(TypeReferenceDefinition type) =>
        type.Subject is not null &&
        type.TargetArtifactKind is null or ArtifactKind.Concept &&
        conceptNames.TryGetValue(type.Subject.Value, out var conceptName)
            ? conceptName
            : type.Name;

    public SpecificationSyntax[] SpecificationsFor(ArtifactPlacement placement) =>
        SpecificationSyntaxLowerer.Lower(
            graph,
            placement,
            artifact => ArtifactName(artifact.Subject, artifact.Kind),
            Sink.Diagnostics,
            Sink.Coverage);
}
