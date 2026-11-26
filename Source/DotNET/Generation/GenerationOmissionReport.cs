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
        IEnumerable<(GenerationFactRecord Record, ImmutableArray<ReportedDiagnostic> Reported)> records)
    {
        var reported = new List<GenerationDiagnostic>();
        var associations = new Dictionary<string, GenerationDiagnostic>(StringComparer.Ordinal);
        var relationships = new Dictionary<(RelationshipKind Kind, string Source, string Target), GenerationDiagnostic>();
        foreach (var (record, entries) in records.Where(pair => pair.Record.Lineage is null))
        {
            foreach (var (diagnostic, original) in entries)
            {
                if (original is not null)
                {
                    associations.TryAdd(Canonical.Diagnostic(original), original);
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

        return [.. reported, .. associations.Values, .. relationships.Values];
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
