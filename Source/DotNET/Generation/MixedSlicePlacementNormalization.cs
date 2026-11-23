// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Retains an exact placement's two-stage rename and attributable base evidence.</summary>
public sealed record PlacementNormalizationRecord
{
    /// <summary>Gets the exact artifact role and backend scope.</summary>
    public required ArtifactKey Artifact { get; init; }

    /// <summary>Gets the source-derived placement before normalization.</summary>
    public required ArtifactPlacement Original { get; init; }

    /// <summary>Gets the placement after applying the explicit artifact fallback.</summary>
    public required ArtifactPlacement AfterArtifact { get; init; }

    /// <summary>Gets the final effective placement after the optional kind suffix.</summary>
    public required ArtifactPlacement Final { get; init; }

    /// <summary>Gets the attributable explicit fallback.</summary>
    public required MixedSlicePlacementFallbackDefinition Fallback { get; init; }

    /// <summary>Gets canonical admitted base fact identities.</summary>
    public IReadOnlyList<FactId> Inputs { get; init; } = [];

    /// <summary>Gets original evidence, including fallback evidence.</summary>
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];
}

static class MixedSlicePlacementNormalization
{
    public static Result Apply(ResolvedArtifactPlacement[] placements, ResolvedArtifact[] artifacts, GenerationFact[] facts, List<GenerationDiagnostic> diagnostics)
    {
        var validArtifacts = artifacts.Where(artifact => !artifact.IsConflicted).Select(artifact => artifact.Key).ToHashSet();
        var entries = placements.Where(placement => !placement.IsConflicted && validArtifacts.Contains(placement.Artifact))
            .Select(Entry.From).Where(entry => IsKnownKind(entry.Final.SliceKind)).ToArray();
        var originalGroups = MixedGroups(entries).Where(group => group.Any(entry => entry.Fallback is not null)).ToArray();
        var opted = originalGroups.SelectMany(group => group).Where(entry => entry.Fallback is not null).Select(entry => entry.Resolved.Artifact).ToHashSet();
        entries = [.. entries.Select(entry => opted.Contains(entry.Resolved.Artifact) ? entry with { AfterArtifact = entry.Final with { Slice = entry.Fallback!.Slice ?? entry.Final.Slice }, Final = entry.Final with { Slice = entry.Fallback!.Slice ?? entry.Final.Slice } } : entry)];
        var secondGroups = MixedGroups(entries);
        var stillMixed = secondGroups.SelectMany(group => group).Where(entry => entry.Fallback is not null).Select(entry => entry.Resolved.Artifact).ToHashSet();
        entries = [.. entries.Select(entry => stillMixed.Contains(entry.Resolved.Artifact) ? entry with { Final = entry.Final with { Slice = entry.Final.Slice + KindToken(entry.Final.SliceKind) } } : entry)];
        var changed = entries.Where(entry => Structural.Placement(entry.Original) != Structural.Placement(entry.Final)).ToDictionary(entry => entry.Resolved.Artifact);
        var groups = originalGroups.Concat(secondGroups).ToArray();
        var records = changed.Values.OrderBy(entry => Structural.ArtifactKey(entry.Resolved.Artifact), StringComparer.Ordinal).Select(entry => Record(entry, groups, facts)).ToArray();
        foreach (var group in originalGroups) diagnostics.Add(Diagnostic(group, facts, GenerationDiagnosticCodes.MixedSliceKindsSplit, "Applied explicit artifact fallback and, only where still mixed, known-kind suffix stages"));
        foreach (var group in MixedGroups(entries).Where(group => group.Any(entry => changed.ContainsKey(entry.Resolved.Artifact)))) diagnostics.Add(Diagnostic(group, facts, GenerationDiagnosticCodes.ConflictingMixedSliceSplit, "Mixed slice collision remains after exactly two stages"));
        return new(
            [.. placements.Select(placement => changed.TryGetValue(placement.Artifact, out var entry) ? placement with { Variants = [.. placement.Variants.Select(entry.NormalizeVariant)] } : placement)],
            records);
    }

    public static string GroupKey(ArtifactKey artifact, ArtifactPlacement placement) => Structural.WithScopes(Structural.SemanticKey("slice-group", placement.Module, Structural.SemanticKey("features", [.. placement.Features]), placement.Slice), artifact.Scope);

    static IGrouping<string, Entry>[] MixedGroups(Entry[] entries) => [.. entries.GroupBy(entry => GroupKey(entry.Resolved.Artifact, entry.Final), StringComparer.Ordinal).Where(group => group.Select(entry => entry.Final.SliceKind).Distinct().Count() > 1).OrderBy(group => group.Key, StringComparer.Ordinal)];

    static bool IsKnownKind(GenerationSliceKind kind) => kind is GenerationSliceKind.StateChange or GenerationSliceKind.StateView or GenerationSliceKind.Automation or GenerationSliceKind.Translate;

    static string KindToken(GenerationSliceKind kind) => kind switch
    {
        GenerationSliceKind.StateChange => "StateChange",
        GenerationSliceKind.StateView => "StateView",
        GenerationSliceKind.Automation => "Automation",
        GenerationSliceKind.Translate => "Translate",
        _ => string.Empty
    };

    static PlacementNormalizationRecord Record(Entry entry, IGrouping<string, Entry>[] groups, GenerationFact[] facts)
    {
        var related = groups.Where(group => group.Any(member => member.Resolved.Artifact == entry.Resolved.Artifact)).SelectMany(group => group);
        var inputs = SupportingFacts(related, facts);
        return new()
        {
            Artifact = entry.Resolved.Artifact,
            Original = entry.Original,
            AfterArtifact = entry.AfterArtifact,
            Final = entry.Final,
            Fallback = MixedSliceFallbackStructure.Copy(entry.Fallback)!,
            Inputs = [.. inputs.Select(fact => fact.Id)],
            Evidence = [.. inputs.Select(fact => fact.Evidence)
                .Concat(inputs.OfType<ArtifactPlacementFact>().Select(fact => fact.MixedSliceFallback?.Evidence).OfType<Evidence>())
                .DistinctBy(Structural.Evidence).OrderBy(Structural.Evidence, StringComparer.Ordinal)]
        };
    }

    static GenerationDiagnostic Diagnostic(IEnumerable<Entry> group, GenerationFact[] facts, string code, string message)
    {
        var entries = group.OrderBy(entry => Structural.ArtifactKey(entry.Resolved.Artifact), StringComparer.Ordinal).ToArray();
        var inputs = SupportingFacts(entries, facts);
        var placement = entries[0].Final;
        var kinds = entries.Select(entry => KindToken(entry.Final.SliceKind)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        return new()
        {
            Code = code,
            Severity = code == GenerationDiagnosticCodes.MixedSliceKindsSplit ? GenerationDiagnosticSeverity.Warning : GenerationDiagnosticSeverity.Error,
            Outcome = GenerationDiagnosticOutcome.Conflict,
            Message = $"{message}: '{placement.Module}/{string.Join('/', placement.Features)}/{placement.Slice}' ({string.Join(", ", kinds)})",
            Subject = entries[0].Resolved.Artifact.Subject,
            Source = inputs.FirstOrDefault(fact => fact.Evidence.Source is not null)?.Evidence.Source,
            Facts = [.. inputs.Select(fact => fact.Id)]
        };
    }

    static GenerationFact[] SupportingFacts(IEnumerable<Entry> entries, GenerationFact[] facts)
    {
        var keys = entries.Select(entry => entry.Resolved.Artifact).ToHashSet();
        return SourcePlacementCandidateDerivation.CanonicalInputs(facts.Where(fact => fact switch
        {
            ArtifactPlacementFact placement => keys.Contains(placement.Artifact),
            ArtifactFact artifact => keys.Contains(artifact.Definition.Key),
            ArtifactDeclarationFact declaration => keys.Contains(declaration.Definition.Artifact),
            _ => false
        }));
    }

    internal sealed record Result(ResolvedArtifactPlacement[] Placements, PlacementNormalizationRecord[] Records);

    sealed record Entry(ResolvedArtifactPlacement Resolved, ArtifactPlacement AfterArtifact, ArtifactPlacement Final)
    {
        public ResolvedArtifactPlacementVariant Variant => Resolved.EffectiveVariants[0];
        public ArtifactPlacement Original => Variant.Placement;
        public MixedSlicePlacementFallbackDefinition? Fallback => Variant.MixedSliceFallback;
        public static Entry From(ResolvedArtifactPlacement placement) => new(placement, placement.EffectiveVariants[0].Placement, placement.EffectiveVariants[0].Placement);
        public ResolvedArtifactPlacementVariant NormalizeVariant(ResolvedArtifactPlacementVariant variant) => variant.Strength == Variant.Strength ? variant with { Placement = Final } : variant;
    }
}
