// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class BackendScopeLowering
{
    public static ResolvedApplicationGraph Prepare(ResolvedApplicationGraph graph, LoweringSink sink)
    {
        foreach (var scope in graph.BackendScopes)
        {
            sink.Diagnostics.Add(BackendScopeFactResolver.Diagnostic(
                scope,
                GenerationDiagnosticCodes.BackendScopeNotSupported,
                GenerationDiagnosticOutcome.Unsupported,
                "Backend store/event-model scope is not supported in .play; only structural declarations and relationships can be emitted"));
        }

        var invalidScopes = graph.BackendScopes.Where(scope => scope.IsConflicted || scope.Definitions.Count == 0 || InvalidDeclaration(scope)).Select(scope => scope.Key).ToHashSet();
        var collisions = CollidingOccurrences(graph).ToArray();
        var suppressed = collisions.SelectMany(group => group.Select(artifact => artifact.Key)).ToHashSet();
        foreach (var artifact in graph.Artifacts)
        {
            if (InvalidOccurrence(artifact.Key, invalidScopes))
            {
                suppressed.Add(artifact.Key);
            }
        }

        AddDependentOccurrences(graph, suppressed);
        foreach (var artifact in graph.Artifacts.Where(artifact => suppressed.Contains(artifact.Key)))
        {
            var participants = collisions.Where(group => group.Any(candidate => candidate.Key == artifact.Key))
                .SelectMany(group => group).DistinctBy(candidate => candidate.Key).ToArray();
            Omit(artifact, participants.Length == 0 ? [artifact] : participants, graph, sink);
        }

        return graph with
        {
            Artifacts = [.. graph.Artifacts.Where(artifact => !suppressed.Contains(artifact.Key))],
            Placements = [.. graph.Placements.Where(placement => !suppressed.Contains(placement.Artifact))]
        };
    }

    static bool InvalidDeclaration(ResolvedBackendScope scope) =>
        scope.Facts.OfType<BackendScopeFact>().Any(fact => fact.Subject != fact.Definition.Key.Store || BackendScopeFactResolver.HasInvalidUsage(fact));

    static IEnumerable<ResolvedArtifact[]> CollidingOccurrences(ResolvedApplicationGraph graph) =>
        graph.Artifacts.Where(artifact => !artifact.IsConflicted)
            .SelectMany(artifact => graph.Placements.Where(placement => placement.Artifact == artifact.Key && !placement.IsConflicted)
                .SelectMany(placement => placement.EffectiveVariants.Select(variant => new
                {
                    Artifact = artifact,
                    Namespace = OutputIdentity(artifact, variant.Placement)
                })))
            .GroupBy(occurrence => occurrence.Namespace, StringComparer.Ordinal)
            .Where(group => group.Select(occurrence => occurrence.Artifact.Key).Distinct().Count() > 1 &&
                            group.Any(occurrence => occurrence.Artifact.Key.Scope is not null))
            .Select(group => group.Select(occurrence => occurrence.Artifact).DistinctBy(artifact => artifact.Key).ToArray());

    static string OutputIdentity(ResolvedArtifact artifact, ArtifactPlacement placement) =>
        Structural.SemanticKey(
            "backend-output",
            placement.Module,
            Structural.SemanticKey("features", placement.Features.Count == 0 ? ["General"] : [.. placement.Features]),
            placement.Slice,
            artifact.Key.Kind.ToString(),
            artifact.Variants[0].Definition.Name);

    static bool InvalidOccurrence(ArtifactKey key, HashSet<BackendScopeKey> invalidScopes) =>
        key.Scope is { } scope && (invalidScopes.Contains(scope) || key.Kind is ArtifactKind.Concept or ArtifactKind.Query);

    static void AddDependentOccurrences(ResolvedApplicationGraph graph, HashSet<ArtifactKey> suppressed)
    {
        bool changed;
        do
        {
            changed = false;
            foreach (var artifact in graph.Artifacts.Where(artifact => !suppressed.Contains(artifact.Key)))
            {
                if (artifact.Variants.SelectMany(variant => variant.Definition.Properties).Any(property => MissingScopedTarget(artifact.Key, property, graph, suppressed)))
                {
                    changed |= suppressed.Add(artifact.Key);
                }
            }
        }
        while (changed);
    }

    static bool MissingScopedTarget(ArtifactKey owner, PropertyDefinition property, ResolvedApplicationGraph graph, HashSet<ArtifactKey> suppressed)
    {
        var type = property.Type;
        if (ConceptOnlyUnbound(owner, property, graph))
        {
            return false;
        }

        var targets = graph.Artifacts.Where(artifact => artifact.Key.Subject == type.Subject).ToArray();
        if (!targets.Any(artifact => artifact.Key.Scope is not null) && type.Scope is null)
        {
            return false;
        }

        return !targets.Any(artifact => artifact.Key.Scope == type.Scope &&
                                        (type.TargetArtifactKind is null || artifact.Key.Kind == type.TargetArtifactKind) &&
                                        !suppressed.Contains(artifact.Key));
    }

    static bool ConceptOnlyUnbound(ArtifactKey owner, PropertyDefinition property, ResolvedApplicationGraph graph) =>
        property.Type.Subject is null && graph.ConceptOnlyMembers.Contains(new() { Artifact = owner, Name = property.Name });

    static void Omit(ResolvedArtifact artifact, ResolvedArtifact[] participants, ResolvedApplicationGraph graph, LoweringSink sink)
    {
        var definitions = artifact.Variants.Select(variant => variant.Definition);
        var diagnostic = new GenerationDiagnostic
        {
            Code = GenerationDiagnosticCodes.BackendScopeOutputCollision,
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = GenerationDiagnosticOutcome.Unsupported,
            Message = "The artifact occurrence was omitted because backend scope identity cannot be represented safely in current Screenplay; no occurrence was merged, renamed, or selected",
            Subject = artifact.Key.Subject,
            Source = LoweringSupport.FirstSource(participants.SelectMany(participant => participant.Variants).SelectMany(variant => variant.Evidence)),
            Facts = [.. participants.SelectMany(participant => participant.Variants).SelectMany(variant => variant.SupportingFacts).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)]
        };
        sink.Omit(diagnostic, [.. definitions.Select(GenerationFactSemanticKey.Artifact)]);
        foreach (var placement in graph.Placements.Where(placement => placement.Artifact == artifact.Key))
        {
            foreach (var variant in placement.EffectiveVariants)
            {
                sink.Omit(diagnostic, GenerationFactSemanticKey.Placement(placement.Artifact, variant.Placement));
            }
        }
    }
}
