// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

internal static class GenerationFactDispositionCalculator
{
    public static ImmutableArray<GenerationFactRecord> Calculate(
        IEnumerable<GenerationFactRecord> records,
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        var inputs = records.ToArray();
        var calculated = Calculate(inputs.Select(record => record.Fact), graph, coverage, diagnostics);
        return
        [
            .. calculated.Select((record, index) => record with { Lineage = inputs[index].Lineage })
        ];
    }

    public static ImmutableArray<GenerationFactRecord> Calculate(
        IEnumerable<GenerationFact> facts,
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        var admitted = facts.ToArray();
        var conflictingIdentities = admitted
            .Where(GenerationFactDiscriminators.AreSupported)
            .GroupBy(_ => _.Id.Value, StringComparer.Ordinal)
            .Where(group => group
                .Select(SemanticIdentity)
                .Distinct(StringComparer.Ordinal)
                .Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);
        var context = new CalculationContext(graph, coverage, diagnostics);

        return
        [
            .. admitted.Select(fact => Calculate(fact, conflictingIdentities.Contains(fact.Id.Value), context))
        ];
    }

    static GenerationFactRecord Calculate(GenerationFact fact, bool hasConflictingIdentity, CalculationContext context)
    {
        if (!GenerationFactDiscriminators.AreSupported(fact))
        {
            return Omitted(fact, context);
        }

        if (hasConflictingIdentity || IsConflicted(fact, context))
        {
            return Conflicted(fact, context);
        }

        return GranularDisposition(fact, context) ?? ClassifiedBySemanticKey(fact, context);
    }

    static bool IsConflicted(GenerationFact fact, CalculationContext context) =>
        GenerationFactConflictDetector.IsConflicted(fact, context.Graph, context.Coverage) ||
        AssociatedDiagnostics(fact, context).Any(diagnostic => diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict);

    static GenerationFactRecord ClassifiedBySemanticKey(GenerationFact fact, CalculationContext context)
    {
        if (fact is ArtifactFact legacyArtifact && IsSupersededByLoweredVariant(legacyArtifact, context))
        {
            return Record(fact, GenerationFactDisposition.ProvenanceOnly);
        }

        var key = GenerationFactSemanticKey.For(fact);
        if (key is not null && context.Coverage.Lowered.Contains(key))
        {
            return Record(fact, GenerationFactDisposition.Lowered);
        }

        if (fact is ArtifactPlacementFact placement && IsWeakerPlacement(placement, context.Graph))
        {
            return Record(fact, GenerationFactDisposition.ProvenanceOnly);
        }

        return key is not null
            ? Omitted(fact, context)
            : Unclassified(fact);
    }

    static GenerationFactRecord Record(GenerationFact fact, GenerationFactDisposition disposition) => new()
    {
        Fact = fact,
        Disposition = disposition
    };

    static bool IsSupersededByLoweredVariant(ArtifactFact legacyArtifact, CalculationContext context) =>
        context.Graph.Artifacts
            .Where(resolved => resolved.Key == legacyArtifact.Definition.Key)
            .SelectMany(resolved => resolved.Variants)
            .Any(variant =>
                Structural.Artifact(legacyArtifact.Definition) != Structural.Artifact(variant.Definition) &&
                variant.SupportingFacts.Any(id => id == legacyArtifact.Id) &&
                context.Coverage.Lowered.Contains(GenerationFactSemanticKey.Artifact(variant.Definition)));

    static GenerationFactRecord Unclassified(GenerationFact fact)
    {
        var diagnostic = new GenerationDiagnostic
        {
            Code = GenerationDiagnosticCodes.UnclassifiedGenerationFact,
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = GenerationDiagnosticOutcome.Unknown,
            Message = $"Admitted fact '{fact.Id.Value}' uses unclassifiable fact type '{fact.GetType().FullName}'",
            Facts = [fact.Id],
            Source = fact.Evidence.Source,
            Subject = fact.Subject
        };
        return new GenerationFactRecord
        {
            Fact = fact,
            Disposition = GenerationFactDisposition.Unknown,
            Diagnostics = [diagnostic]
        };
    }

    static GenerationFactRecord Conflicted(GenerationFact fact, CalculationContext context)
    {
        var associated = AssociatedDiagnostics(fact, context)
            .Where(_ => _.Outcome == GenerationDiagnosticOutcome.Conflict)
            .ToArray();
        if (associated.Length == 0)
        {
            associated = [ConflictingFact(fact)];
        }

        return new GenerationFactRecord
        {
            Fact = fact,
            Disposition = GenerationFactDisposition.Conflicted,
            Diagnostics = CanonicalDiagnostics(associated)
        };
    }

    static GenerationDiagnostic ConflictingFact(GenerationFact fact) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingGenerationFact,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Conflict,
        Message = $"Admitted fact '{fact.Id.Value}' participated in an unresolved semantic conflict",
        Facts = [fact.Id],
        Source = fact.Evidence.Source,
        Subject = GenerationFactDescription.DiagnosticSubject(fact)
    };

    static GenerationFactRecord Omitted(GenerationFact fact, CalculationContext context)
    {
        var associated = AssociatedDiagnostics(fact, context).ToList();
        if (fact is RelationshipFact relationship && GenerationFactDiscriminators.AreSupported(fact))
        {
            associated.Add(UnsupportedRelationship(relationship));
        }

        if (associated.Count == 0)
        {
            associated.Add(OmittedFact(fact));
        }

        return new GenerationFactRecord
        {
            Fact = fact,
            Disposition = GenerationFactDisposition.OmittedWithDiagnostic,
            Diagnostics = CanonicalDiagnostics(associated)
        };
    }

    static GenerationDiagnostic UnsupportedRelationship(RelationshipFact relationship) => new()
    {
        Code = GenerationDiagnosticCodes.UnsupportedRelationship,
        Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = GenerationDiagnosticOutcome.Unsupported,
        Message = $"Relationship '{relationship.Definition.Key.Kind}' from '{relationship.Definition.Key.Source.Value}' to '{relationship.Definition.Key.Target.Value}' did not contribute to emitted Screenplay syntax and was omitted",
        Facts = [relationship.Id],
        Source = relationship.Evidence.Source,
        Subject = relationship.Definition.Key.Source
    };

    static GenerationDiagnostic OmittedFact(GenerationFact fact) => new()
    {
        Code = GenerationDiagnosticCodes.OmittedGenerationFact,
        Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = GenerationDiagnosticOutcome.Unsupported,
        Message = $"Admitted {GenerationFactDescription.Family(fact)} fact '{fact.Id.Value}' did not contribute to emitted Screenplay syntax and was omitted",
        Facts = [fact.Id],
        Source = fact.Evidence.Source,
        Subject = GenerationFactDescription.DiagnosticSubject(fact)
    };

    static IEnumerable<GenerationDiagnostic> AssociatedDiagnostics(GenerationFact fact, CalculationContext context)
    {
        var key = GenerationFactSemanticKey.For(fact);
        if (key is not null && context.Coverage.Diagnostics.TryGetValue(key, out var loweringDiagnostics))
        {
            foreach (var diagnostic in loweringDiagnostics)
            {
                yield return Associate(diagnostic, fact.Id);
            }
        }

        foreach (var diagnostic in context.Diagnostics.Where(diagnostic =>
                     diagnostic.Outcome is not null && diagnostic.Facts.Any(link => link == fact.Id)))
        {
            yield return diagnostic;
        }
    }

    static GenerationFactRecord? GranularDisposition(GenerationFact fact, CalculationContext context)
    {
        var artifact = GranularArtifact(fact);
        if (artifact is null)
        {
            return null;
        }

        var appliedVariants = context.Graph.Artifacts
            .Where(resolved => resolved.Key == artifact)
            .SelectMany(resolved => resolved.Variants)
            .Where(variant => variant.SupportingFacts.Any(id => id == fact.Id))
            .ToArray();
        if (appliedVariants.Length == 0)
        {
            return Omitted(fact, context);
        }

        var lowered = appliedVariants.Any(variant =>
            context.Coverage.Lowered.Contains(GenerationFactSemanticKey.Artifact(variant.Definition)));
        return Record(
            fact,
            ContributesSyntax(fact, lowered) ? GenerationFactDisposition.Lowered : GenerationFactDisposition.ProvenanceOnly);
    }

    static ArtifactKey? GranularArtifact(GenerationFact fact) => fact switch
    {
        ArtifactDeclarationFact declaration => declaration.Definition.Artifact,
        ArtifactMemberDeclarationFact member => member.Definition.Member.Artifact,
        ArtifactMemberTypeUseFact typeUse => typeUse.Definition.Member.Artifact,
        TypeUseBindingFact binding => binding.Definition.Member.Artifact,
        ArtifactMemberRoleFact role => role.Definition.Member.Artifact,
        _ => null
    };

    static bool ContributesSyntax(GenerationFact fact, bool lowered) => fact switch
    {
        TypeUseBindingFact => lowered,
        ArtifactMemberRoleFact role =>
            lowered && role.Definition.Role == ArtifactMemberRoleKind.EventSourceIdentifier,
        _ => false
    };

    static bool IsWeakerPlacement(ArtifactPlacementFact fact, ResolvedApplicationGraph graph)
    {
        var resolved = graph.Placements.FirstOrDefault(_ =>
            Structural.ArtifactKey(_.Artifact) == Structural.ArtifactKey(fact.Artifact));
        if (resolved?.Variants.Any(_ => Structural.Placement(_.Placement) == Structural.Placement(fact.Placement)) != true)
        {
            return false;
        }

        var placement = Structural.Placement(fact.Placement);
        return resolved.EffectiveVariants.All(_ => Structural.Placement(_.Placement) != placement);
    }

    static GenerationDiagnostic Associate(GenerationDiagnostic diagnostic, FactId fact) => diagnostic with
    {
        Facts =
        [
            .. diagnostic.Facts
                .Append(fact)
                .Distinct()
                .OrderBy(link => link.Value, StringComparer.Ordinal)
        ]
    };

    static string SemanticIdentity(GenerationFact fact) =>
        GenerationFactSemanticKey.For(fact) ?? fact.GetType().FullName ?? fact.GetType().Name;

    static ImmutableArray<GenerationDiagnostic> CanonicalDiagnostics(IEnumerable<GenerationDiagnostic> diagnostics) =>
    [
        .. diagnostics
            .GroupBy(Canonical.Diagnostic, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.First())
    ];

    sealed record CalculationContext(
        ResolvedApplicationGraph Graph,
        ScreenplayLoweringCoverage Coverage,
        IReadOnlyList<GenerationDiagnostic> Diagnostics);
}
