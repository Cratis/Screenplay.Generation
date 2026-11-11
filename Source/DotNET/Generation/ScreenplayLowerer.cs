// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using static Cratis.Screenplay.Generation.LoweringSupport;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Represents the syntax tree and diagnostics produced while lowering a resolved graph.
/// </summary>
public sealed record ScreenplayLoweringResult
{
    /// <summary>
    /// Gets the generated Screenplay syntax tree.
    /// </summary>
    public required ApplicationSyntax Application { get; init; }

    /// <summary>
    /// Gets diagnostics produced while lowering the graph.
    /// </summary>
    public IReadOnlyList<GenerationDiagnostic> Diagnostics { get; init; } = [];

    internal ScreenplayLoweringCoverage Coverage { get; init; } = ScreenplayLoweringCoverage.Empty;
}

/// <summary>
/// Lowers a resolved semantic application graph into the canonical Screenplay syntax tree.
/// </summary>
public sealed class ScreenplayLowerer
{
    /// <summary>
    /// Lowers the supplied graph.
    /// </summary>
    /// <param name="graph">The resolved application graph.</param>
    /// <param name="domain">The generated Screenplay domain name.</param>
    /// <returns>The lowering result.</returns>
    public ScreenplayLoweringResult Lower(ResolvedApplicationGraph graph, string domain)
    {
        var sink = new LoweringSink();
        ResolverDiagnosticCoverage.Propagate(graph, sink.Coverage);
        ReportUnsupportedRelationships(graph, sink);
        var placements = graph.Placements
            .Where(_ => !_.IsConflicted)
            .ToDictionary(_ => Structural.ArtifactKey(_.Artifact), _ => _.EffectiveVariants.Single().Placement, StringComparer.Ordinal);
        var definitions = graph.Artifacts
            .Where(_ => !_.IsConflicted)
            .Select(_ => _.Variants.Single().Definition)
            .ToArray();
        var (concepts, conceptNames) = ConceptLowering.Lower(graph, definitions, sink);
        var artifactsWithMissingConceptReferences = ReportMissingConceptReferences(definitions, conceptNames, sink);
        var context = new LoweringContext(graph, conceptNames, sink);
        OmitUnplaced(graph, definitions, placements, sink);
        var artifacts = PlaceArtifacts(graph, definitions, placements, artifactsWithMissingConceptReferences);
        OmitUnsupported(graph, artifacts, sink);

        ModuleSyntax[] modules =
        [
            .. artifacts
                .Where(_ => CanLower(_.Definition.Key.Kind))
                .GroupBy(_ => _.Placement.Module, StringComparer.Ordinal)
                .OrderBy(_ => _.Key, StringComparer.Ordinal)
                .Select(_ => SliceLowering.BuildModule(_.Key, _, context))
        ];

        return new()
        {
            Application = new ApplicationSyntax(
                [],
                concepts,
                [],
                modules,
                Generated,
                new DomainSyntax(domain, Generated)),
            Diagnostics = [.. sink.Diagnostics.OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)],
            Coverage = sink.Coverage.Build()
        };
    }

    static void OmitUnplaced(
        ResolvedApplicationGraph graph,
        IReadOnlyList<ArtifactDefinition> definitions,
        Dictionary<string, ArtifactPlacement> placements,
        LoweringSink sink)
    {
        foreach (var unplaced in definitions.Where(_ =>
                     _.Key.Kind != ArtifactKind.Concept &&
                     !placements.ContainsKey(Structural.ArtifactKey(_.Key))))
        {
            sink.Omit(
                UnplacedArtifactDiagnostic(unplaced, SourceForArtifact(graph, unplaced.Key)),
                GenerationFactSemanticKey.Artifact(unplaced));
        }
    }

    static PlacedArtifact[] PlaceArtifacts(
        ResolvedApplicationGraph graph,
        IReadOnlyList<ArtifactDefinition> definitions,
        Dictionary<string, ArtifactPlacement> placements,
        HashSet<string> excluded) =>
        [
            .. definitions
                .Where(_ => placements.ContainsKey(Structural.ArtifactKey(_.Key)) &&
                            !excluded.Contains(Structural.ArtifactKey(_.Key)))
                .Select(_ => new PlacedArtifact(
                    _,
                    placements[Structural.ArtifactKey(_.Key)],
                    SourceForPlacement(graph, _.Key)))
                .OrderBy(_ => Canonical.Artifact(_.Definition), StringComparer.Ordinal)
        ];

    static void OmitUnsupported(ResolvedApplicationGraph graph, IReadOnlyList<PlacedArtifact> artifacts, LoweringSink sink)
    {
        foreach (var unsupported in artifacts.Where(_ =>
                     _.Definition.Key.Kind != ArtifactKind.Concept &&
                     !CanLower(_.Definition.Key.Kind)))
        {
            sink.Omit(
                ArtifactKindProblem(unsupported.Definition, SourceForArtifact(graph, unsupported.Definition.Key))!,
                GenerationFactSemanticKey.Artifact(unsupported.Definition),
                GenerationFactSemanticKey.Placement(unsupported.Definition.Key, unsupported.Placement));
        }
    }

    static HashSet<string> ReportMissingConceptReferences(
        IReadOnlyList<ArtifactDefinition> definitions,
        IReadOnlyDictionary<string, string> conceptNames,
        LoweringSink sink)
    {
        var missing = definitions
            .SelectMany(definition => definition.Properties
                .Where(property => property.Type.Subject is not null && !conceptNames.ContainsKey(property.Type.Subject.Value))
                .Select(property => new { Artifact = definition, property.Type }))
            .ToArray();
        foreach (var type in missing
                     .GroupBy(_ => _.Type.Subject!.Value, StringComparer.Ordinal)
                     .OrderBy(_ => _.Key, StringComparer.Ordinal)
                     .Select(_ => _.First().Type))
        {
            sink.Diagnostics.Add(
                Warning(
                    GenerationDiagnosticCodes.MissingConceptReference,
                    GenerationDiagnosticOutcome.Unknown,
                    $"Type reference '{type.Name}' targets concept '{type.Subject!.Value}', which could not be emitted") with
                {
                    Subject = type.Subject
                });
        }

        foreach (var definition in missing
                     .Select(_ => _.Artifact)
                     .DistinctBy(Canonical.Artifact, StringComparer.Ordinal))
        {
            var diagnostic = sink.Diagnostics.First(_ =>
                _.Code == GenerationDiagnosticCodes.MissingConceptReference &&
                definition.Properties.Any(property => property.Type.Subject == _.Subject));
            sink.Coverage.Omitted(GenerationFactSemanticKey.Artifact(definition), diagnostic);
        }

        return [.. missing.Select(_ => Structural.ArtifactKey(_.Artifact.Key))];
    }

    static void ReportUnsupportedRelationships(ResolvedApplicationGraph graph, LoweringSink sink)
    {
        foreach (var relationship in graph.Relationships.Where(_ =>
                     _.Key.Kind == RelationshipKind.Unknown ||
                     !Enum.IsDefined(_.Key.Kind)))
        {
            sink.Diagnostics.Add(
                Warning(
                    GenerationDiagnosticCodes.UnsupportedRelationshipKind,
                    OutcomeFor(relationship.Key.Kind, RelationshipKind.Unknown),
                    $"Relationship from '{relationship.Key.Source.Value}' to '{relationship.Key.Target.Value}' uses unknown or undefined RelationshipKind value '{(int)relationship.Key.Kind}' and was omitted") with
                {
                    Source = FirstSource(relationship.Evidence),
                    Subject = relationship.Key.Source
                });
        }
    }

    static GenerationDiagnostic UnplacedArtifactDiagnostic(ArtifactDefinition artifact, SourceRange? source) =>
        ArtifactKindProblem(artifact, source) ??
        Warning(
            GenerationDiagnosticCodes.IncompleteArtifact,
            GenerationDiagnosticOutcome.Unknown,
            $"The recognized {artifact.Key.Kind} artifact '{artifact.Name}' has no Screenplay placement and was omitted") with
        {
            Source = source,
            Subject = artifact.Key.Subject
        };

    static GenerationDiagnostic? ArtifactKindProblem(ArtifactDefinition artifact, SourceRange? source)
    {
        if (CanLower(artifact.Key.Kind))
        {
            return null;
        }

        var diagnostic = IsKnownArtifactKind(artifact.Key.Kind)
            ? Warning(
                GenerationDiagnosticCodes.UnsupportedArtifact,
                GenerationDiagnosticOutcome.Unsupported,
                $"The recognized {artifact.Key.Kind} artifact '{artifact.Name}' cannot yet be represented by the Screenplay lowerer and was omitted")
            : Warning(
                GenerationDiagnosticCodes.UnsupportedArtifactKind,
                OutcomeFor(artifact.Key.Kind, ArtifactKind.Unknown),
                $"Artifact '{artifact.Name}' uses unknown or undefined ArtifactKind value '{(int)artifact.Key.Kind}' and was omitted");
        return diagnostic with { Source = source, Subject = artifact.Key.Subject };
    }

    static SourceRange? SourceForArtifact(ResolvedApplicationGraph graph, ArtifactKey key) =>
        FirstSource(graph.Artifacts
            .Where(_ => Structural.ArtifactKey(_.Key) == Structural.ArtifactKey(key))
            .SelectMany(_ => _.Variants)
            .SelectMany(_ => _.Evidence));

    static SourceRange? SourceForPlacement(ResolvedApplicationGraph graph, ArtifactKey key) =>
        FirstSource(graph.Placements
            .Where(_ => Structural.ArtifactKey(_.Artifact) == Structural.ArtifactKey(key))
            .SelectMany(_ => _.EffectiveVariants)
            .SelectMany(_ => _.Evidence));
}
