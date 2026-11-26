// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Builds the outward diagnostics for fact records, counting omitted artifacts and relationships once
/// instead of once per admitted fact; the per-fact diagnostics stay on their records.
/// </summary>
internal static class GenerationOmissionReport
{
    public static ImmutableArray<GenerationDiagnostic> Diagnostics(
        IEnumerable<GenerationFactRecord> records,
        ScreenplayLoweringCoverage coverage)
    {
        var artifactOmissions = coverage.Diagnostics.Values
            .SelectMany(diagnostics => diagnostics)
            .Where(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.UnsupportedArtifact)
            .ToArray();
        var reported = new List<GenerationDiagnostic>();
        var artifacts = new Dictionary<string, GenerationDiagnostic>(StringComparer.Ordinal);
        var relationships = new Dictionary<(RelationshipKind Kind, string Source, string Target), GenerationDiagnostic>();
        foreach (var record in records.Where(record => record.Lineage is null))
        {
            foreach (var diagnostic in record.Diagnostics)
            {
                if (ArtifactOmission(diagnostic, artifactOmissions) is { } artifact)
                {
                    artifacts.TryAdd(Canonical.Diagnostic(artifact), artifact);
                }
                else if (RelationshipOmission(record.Fact, diagnostic) is { } relationship)
                {
                    relationships.TryAdd(RelationshipTuple(relationship.Key), relationship.Diagnostic);
                }
                else
                {
                    reported.Add(diagnostic);
                }
            }
        }

        return [.. reported, .. artifacts.Values, .. relationships.Values];
    }

    public static GenerationDiagnostic UnsupportedRelationship(RelationshipFact relationship) => new()
    {
        Code = GenerationDiagnosticCodes.UnsupportedRelationship,
        Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = GenerationDiagnosticOutcome.Unsupported,
        Message = $"Relationship '{relationship.Definition.Key.Kind}' from '{relationship.Definition.Key.Source.Value}' to '{relationship.Definition.Key.Target.Value}' did not contribute to emitted Screenplay syntax and was omitted",
        Facts = [relationship.Id],
        Source = relationship.Evidence.Source,
        Subject = relationship.Definition.Key.Source
    };

    static GenerationDiagnostic? ArtifactOmission(GenerationDiagnostic diagnostic, GenerationDiagnostic[] artifactOmissions) =>
        diagnostic.Code == GenerationDiagnosticCodes.UnsupportedArtifact
            ? artifactOmissions.FirstOrDefault(original =>
                Canonical.Diagnostic(diagnostic with { Facts = original.Facts }) == Canonical.Diagnostic(original))
            : null;

    static (RelationshipKey Key, GenerationDiagnostic Diagnostic)? RelationshipOmission(GenerationFact fact, GenerationDiagnostic diagnostic)
    {
        if (fact is not RelationshipFact relationship ||
            diagnostic.Code != GenerationDiagnosticCodes.UnsupportedRelationship ||
            Canonical.Diagnostic(diagnostic) != Canonical.Diagnostic(UnsupportedRelationship(relationship)))
        {
            return null;
        }

        return (relationship.Definition.Key, diagnostic with { Facts = [], Source = null, Lineage = null });
    }

    static (RelationshipKind Kind, string Source, string Target) RelationshipTuple(RelationshipKey key) =>
        (key.Kind, key.Source.Value, key.Target.Value);
}
