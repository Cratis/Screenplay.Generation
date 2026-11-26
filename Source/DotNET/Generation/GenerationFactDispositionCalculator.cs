// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

internal static class GenerationFactDispositionCalculator
{
    public static CalculatedFactRecords Calculate(
        IEnumerable<GenerationFactRecord> records,
        DispositionInputs dispositionInputs)
    {
        var inputs = records.ToArray();
        var calculated = CalculateWithReport(inputs.Select(record => record.Fact), dispositionInputs);
        return new(
            [.. calculated.Records.Select((record, index) => record with { Lineage = inputs[index].Lineage })],
            calculated.Reported);
    }

    public static ImmutableArray<GenerationFactRecord> Calculate(
        IEnumerable<GenerationFact> facts,
        DispositionInputs dispositionInputs) =>
        CalculateWithReport(facts, dispositionInputs).Records;

    static CalculatedFactRecords CalculateWithReport(
        IEnumerable<GenerationFact> facts,
        DispositionInputs dispositionInputs)
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
        var validScopeFacts = BackendScopeFactResolver.ValidFacts(admitted, []).Select(fact => fact.Id).ToHashSet();
        var context = new CalculationContext(dispositionInputs, validScopeFacts);

        var records = admitted.Select(fact => Calculate(fact, conflictingIdentities.Contains(fact.Id.Value), context)).ToImmutableArray();
        return new(records, [.. records.Select(context.Originals)]);
    }

    static GenerationFactRecord Calculate(GenerationFact fact, bool hasConflictingIdentity, CalculationContext context)
    {
        var record = CalculateStructural(fact, hasConflictingIdentity, context);
        if (record.Disposition != GenerationFactDisposition.Lowered || BackendScopeFactReferences.Of(fact).Count == 0)
        {
            return record;
        }

        var diagnostics = CanonicalDiagnostics(fact, context, AssociatedDiagnostics(fact, context)
            .Where(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.BackendScopeNotSupported));
        return diagnostics.IsEmpty ? record : record with { Disposition = GenerationFactDisposition.PartiallyLowered, Diagnostics = diagnostics };
    }

    static GenerationFactRecord CalculateStructural(GenerationFact fact, bool hasConflictingIdentity, CalculationContext context)
    {
        if (IsDiagnosticIntent(fact))
        {
            return DiagnosticIntentDisposition(fact, context);
        }

        if (!GenerationFactDiscriminators.AreSupported(fact))
        {
            return Omitted(fact, context);
        }

        if (hasConflictingIdentity || IsConflicted(fact, context))
        {
            return Conflicted(fact, context);
        }

        return NeutralDisposition(fact, context) ?? PlacementNormalization.Disposition(fact, context.Graph) ?? GranularDisposition(fact, context) ?? ClassifiedBySemanticKey(fact, context);
    }

    static GenerationFactRecord DiagnosticIntentDisposition(GenerationFact fact, CalculationContext context)
    {
        var diagnostics = CanonicalDiagnostics(fact, context, context.Diagnostics.Where(diagnostic => diagnostic.Facts.Contains(fact.Id) || diagnostic.Lineage?.Inputs.Contains(fact.Id) == true));
        var failures = diagnostics.Where(IsDiagnosticIntentFailure).ToArray();
        var disposition = failures.Length > 0 ? GenerationFactDisposition.OmittedWithDiagnostic : GenerationFactDisposition.ProvenanceOnly;
        if (failures.Any(diagnostic => diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict))
        {
            disposition = GenerationFactDisposition.Conflicted;
        }

        return new() { Fact = fact, Disposition = disposition, Diagnostics = diagnostics };
    }

    static bool IsDiagnosticIntent(GenerationFact fact) => fact is MethodDiagnosticProjectionFact or OccurrenceOmissionDiagnosticFact or ArtifactMemberRoleSelectionFact;

    static bool IsDiagnosticIntentFailure(GenerationDiagnostic diagnostic) =>
        diagnostic.Code == GenerationDiagnosticCodes.InvalidDiagnosticIntent || diagnostic.Code == GenerationDiagnosticCodes.MissingDiagnosticEntryPoint ||
        diagnostic.Code == GenerationDiagnosticCodes.AmbiguousDiagnosticEntryPoint || diagnostic.Code == GenerationDiagnosticCodes.InvalidOccurrenceOmissionDiagnostic ||
        diagnostic.Code == GenerationDiagnosticCodes.ConflictingOccurrenceOmissionDiagnostic;

    static GenerationFactRecord? NeutralDisposition(GenerationFact fact, CalculationContext context)
    {
        if (fact is not (TypeUseCompatibilityProjectionFact or TypeUseBindingPolicyFact or ArtifactSourcePlacementCandidateFact or DeclarationInputMemberTargetOmittedFact or TypeUseTargetOmittedFact or BackendScopeFact or ArtifactUsageDeclarationFact or MethodArtifactUsageFact or EntryPointFact or MethodExecutionClassificationFact or MethodFactSupportFact or DeclarationInputMemberDeclarationFact or DeclarationInputMemberTypeUseFact or EntryPointInputTransferFact or MethodDeclarationExecutionFact))
        {
            return null;
        }

        var diagnostics = CanonicalDiagnostics(fact, context, AssociatedDiagnostics(fact, context));
        return diagnostics.Any(diagnostic => diagnostic.Code != GenerationDiagnosticCodes.BackendScopeNotSupported && diagnostic.Outcome is GenerationDiagnosticOutcome.Unknown or GenerationDiagnosticOutcome.Unsupported)
            ? Omitted(fact, context)
            : new GenerationFactRecord { Fact = fact, Disposition = GenerationFactDisposition.ProvenanceOnly, Diagnostics = diagnostics };
    }

    static bool IsConflicted(GenerationFact fact, CalculationContext context) =>
        GenerationFactConflictDetector.IsConflicted(fact, context.Graph, context.Coverage) ||
        PlacementNormalization.IsConflicted(fact, context.Graph, context.Coverage, AssociatedDiagnostics(fact, context));

    static GenerationFactRecord ClassifiedBySemanticKey(GenerationFact fact, CalculationContext context)
    {
        if (fact is ArtifactFact legacyArtifact && IsSupersededByLoweredVariant(legacyArtifact, context))
        {
            return Record(fact, GenerationFactDisposition.ProvenanceOnly);
        }

        var key = PlacementNormalization.SemanticKey(fact, context.Graph);
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
            Diagnostics = CanonicalDiagnostics(fact, context, associated)
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
            associated.Add(GenerationOmissionReport.UnsupportedRelationship(relationship));
        }

        if (context.Explanations.TryGetValue(GenerationFactInstance.Of(fact), out var explanations))
        {
            associated.AddRange(explanations);
        }

        if (associated.Count == 0)
        {
            associated.Add(OmittedFact(fact));
        }

        return new GenerationFactRecord
        {
            Fact = fact,
            Disposition = GenerationFactDisposition.OmittedWithDiagnostic,
            Diagnostics = CanonicalDiagnostics(fact, context, associated)
        };
    }

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
        var key = PlacementNormalization.SemanticKey(fact, context.Graph);
        if (key is not null && context.Coverage.Diagnostics.TryGetValue(key, out var loweringDiagnostics))
        {
            foreach (var diagnostic in loweringDiagnostics)
            {
                yield return Associate(diagnostic, fact.Id, context);
            }
        }

        foreach (var diagnostic in context.Diagnostics.Where(diagnostic =>
                     diagnostic.Outcome is not null &&
                     diagnostic.Facts.Any(link => link == fact.Id) &&
                     ScopeDiagnosticApplies(fact, diagnostic, context)))
        {
            yield return diagnostic;
        }
    }

    static bool ScopeDiagnosticApplies(GenerationFact fact, GenerationDiagnostic diagnostic, CalculationContext context)
    {
        if (string.Equals(diagnostic.Code, GenerationDiagnosticCodes.UnknownBackendScope, StringComparison.Ordinal) ||
            string.Equals(diagnostic.Code, GenerationDiagnosticCodes.AmbiguousBackendScopeOwnership, StringComparison.Ordinal))
        {
            return !context.ValidScopeFacts.Contains(fact.Id);
        }

        return true;
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
            .ToArray()
            .Where(variant => variant.SupportingFacts.Any(id => id == fact.Id))
            .ToArray();
        if (appliedVariants.Length == 0)
        {
            return Omitted(fact, context);
        }

        var lowered = appliedVariants.Any(variant =>
            context.Coverage.Lowered.Contains(GenerationFactSemanticKey.Artifact(variant.Definition)));
        var record = Record(
            fact,
            ContributesSyntax(fact, lowered) ? GenerationFactDisposition.Lowered : GenerationFactDisposition.ProvenanceOnly);
        return fact is ArtifactDeclarationFact && !lowered
            ? record with { Diagnostics = ArtifactOmissions(fact, appliedVariants, context) }
            : record;
    }

    static ImmutableArray<GenerationDiagnostic> ArtifactOmissions(
        GenerationFact fact,
        IEnumerable<ResolvedArtifactVariant> variants,
        CalculationContext context) =>
        CanonicalDiagnostics(fact, context, variants
            .Select(variant => GenerationFactSemanticKey.Artifact(variant.Definition))
            .Distinct(StringComparer.Ordinal)
            .Where(context.Coverage.Diagnostics.ContainsKey)
            .SelectMany(key => context.Coverage.Diagnostics[key])
            .Where(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.UnsupportedArtifact)
            .Select(diagnostic => Associate(diagnostic, fact.Id, context)));

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

    static GenerationDiagnostic Associate(GenerationDiagnostic diagnostic, FactId fact, CalculationContext context)
    {
        var associated = diagnostic with
        {
            Facts =
            [
                .. diagnostic.Facts
                    .Append(fact)
                    .Distinct()
                    .OrderBy(link => link.Value, StringComparer.Ordinal)
            ]
        };
        context.Origins.Link(associated, diagnostic);
        return associated;
    }

    static string SemanticIdentity(GenerationFact fact) =>
        GenerationFactSemanticKey.For(fact) ?? fact.GetType().FullName ?? fact.GetType().Name;

    static ImmutableArray<GenerationDiagnostic> CanonicalDiagnostics(GenerationFact fact, CalculationContext context, IEnumerable<GenerationDiagnostic> diagnostics)
    {
        var candidates = diagnostics.ToArray();
        context.Pending[fact.Id] = [.. candidates.Select(diagnostic => new ReportedDiagnostic(diagnostic, context.Origins.Of(diagnostic)))];
        return
        [
            .. candidates
                .GroupBy(Canonical.Diagnostic, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => group.First())
        ];
    }

    sealed record CalculationContext(DispositionInputs Inputs, IReadOnlySet<FactId> ValidScopeFacts)
    {
        public ResolvedApplicationGraph Graph => Inputs.Graph;

        public ScreenplayLoweringCoverage Coverage => Inputs.Coverage;

        public IReadOnlyList<GenerationDiagnostic> Diagnostics => Inputs.Diagnostics;

        public ImmutableDictionary<GenerationFactInstance, ImmutableArray<GenerationDiagnostic>> Explanations => Inputs.Explanations;

        /// <summary>Gets the lowering original of each diagnostic instance linked to a fact.</summary>
        public OriginTable Origins { get; } = new();

        /// <summary>Gets the diagnostics each fact considered before payload deduplication, with their origins.</summary>
        public Dictionary<FactId, ImmutableArray<ReportedDiagnostic>> Pending { get; } = [];

        public ImmutableArray<ReportedDiagnostic> Originals(GenerationFactRecord record)
        {
            var kept = record.Diagnostics.Select(Canonical.Diagnostic).ToHashSet(StringComparer.Ordinal);
            return Pending.TryGetValue(record.Fact.Id, out var pending) && pending.Select(entry => Canonical.Diagnostic(entry.Diagnostic)).ToHashSet(StringComparer.Ordinal).SetEquals(kept)
                ? pending
                : [.. record.Diagnostics.Select(diagnostic => new ReportedDiagnostic(diagnostic, null))];
        }
    }
}

/// <summary>
/// Tracks, by diagnostic instance, the lowering original a linked copy was made from.
/// </summary>
internal sealed class OriginTable
{
    readonly System.Runtime.CompilerServices.ConditionalWeakTable<GenerationDiagnostic, GenerationDiagnostic> _origins = [];

    public void Link(GenerationDiagnostic copy, GenerationDiagnostic original) => _origins.AddOrUpdate(copy, original);

    public GenerationDiagnostic? Of(GenerationDiagnostic diagnostic) => _origins.TryGetValue(diagnostic, out var original) ? original : null;
}

/// <summary>
/// Holds the pipeline inputs a fact disposition is calculated from.
/// </summary>
/// <param name="Graph">The resolved application graph.</param>
/// <param name="Coverage">The lowering coverage.</param>
/// <param name="Diagnostics">The pipeline diagnostics.</param>
/// <param name="Explanations">The concept-yield explanations per fact instance.</param>
internal sealed record DispositionInputs(
    ResolvedApplicationGraph Graph,
    ScreenplayLoweringCoverage Coverage,
    IReadOnlyList<GenerationDiagnostic> Diagnostics,
    ImmutableDictionary<GenerationFactInstance, ImmutableArray<GenerationDiagnostic>> Explanations);

/// <summary>
/// Holds calculated fact records with, per record, each diagnostic paired with the lowering original it was
/// associated from, when it is an association.
/// </summary>
/// <param name="Records">The calculated records.</param>
/// <param name="Reported">The reported diagnostics, one array per record at the same index.</param>
internal sealed record CalculatedFactRecords(
    ImmutableArray<GenerationFactRecord> Records,
    ImmutableArray<ImmutableArray<ReportedDiagnostic>> Reported);

/// <summary>
/// Holds a record diagnostic and, when it is a lowering diagnostic linked to the record, the original to publish.
/// </summary>
/// <param name="Diagnostic">The diagnostic as held on the record.</param>
/// <param name="AssociatedFrom">The lowering original, or null for a pass-through diagnostic.</param>
internal sealed record ReportedDiagnostic(GenerationDiagnostic Diagnostic, GenerationDiagnostic? AssociatedFrom);
