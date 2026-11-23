// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Defines supported mixed-kind slice normalization policies.</summary>
public enum MixedSlicePlacementFallbackMode
{
    /// <summary>No supported split policy.</summary>
    Unknown = -1,

    /// <summary>Try the explicit artifact fallback, then append the known slice-kind token.</summary>
    ArtifactThenKind = 0
}

/// <summary>Opts the enclosing exact artifact placement into two-stage mixed-slice splitting.</summary>
public sealed record MixedSlicePlacementFallbackDefinition
{
    /// <summary>Gets the supported policy version.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Gets the two-stage selection mode.</summary>
    public MixedSlicePlacementFallbackMode Mode { get; init; } = MixedSlicePlacementFallbackMode.ArtifactThenKind;

    /// <summary>Gets the optional explicit artifact-specific slice name, without module or feature components.</summary>
    /// <remarks>When absent, the current slice name is retained as the base for the kind suffix.</remarks>
    public string? Slice { get; init; }

    /// <summary>Gets attributable evidence for the fallback name and policy.</summary>
    public required Evidence Evidence { get; init; }
}

static class MixedSliceFallbackAdmission
{
    public static MixedSlicePlacementFallbackDefinition? For(GenerationFact fact) => fact switch
    {
        ArtifactPlacementFact placement => placement.MixedSliceFallback,
        ArtifactSourcePlacementCandidateFact candidate => candidate.Definition.MixedSliceFallback,
        _ => null
    };

    public static MixedSlicePlacementFallbackDefinition? Freeze(MixedSlicePlacementFallbackDefinition? fallback, string path, AdapterContributionAdmissionContext context) => fallback is null ? null : fallback with
    {
        Evidence = AdapterContributionFreezer.FreezeNestedEvidence(fallback.Evidence, $"{path}.Evidence", context)
    };

    public static void Validate(GenerationFact fact, string path, ISourceAuthorityValidator? authority, AdapterContributionAdmissionContext context)
    {
        if (For(fact) is not { } fallback) return;
        context.Enum(fallback.Mode, MixedSlicePlacementFallbackMode.Unknown, $"{path}.MixedSliceFallback.Mode", fact.Id, fact.Subject);
        if (fallback.Version != 1 || !ValidName(fallback.Slice)) context.Add(AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand, $"{path}.MixedSliceFallback", "Mixed-slice fallback requires version 1 and an optional slice name without path components", fact.Id, fact.Subject);
        if (fallback.Evidence.Adapter != fact.Evidence.Adapter) context.Add(AdapterContributionAdmissionDiagnosticCode.EvidenceAdapterMismatch, $"{path}.MixedSliceFallback.Evidence", "Fallback evidence must belong to the contributing adapter", fact.Id, fact.Subject);
        context.Enum(fallback.Evidence.Strength, EvidenceStrength.Unknown, $"{path}.MixedSliceFallback.Evidence.Strength", fact.Id, fact.Subject);
        if (fallback.Evidence.Source is { } source) AdapterContributionAdmissionValidator.ValidateSource(source, $"{path}.MixedSliceFallback.Evidence.Source", fact.Id, fact.Subject, authority, context);
    }

    public static void ValidateCapability(GenerationFact fact, AdapterDescriptor descriptor, string path, AdapterContributionAdmissionContext context)
    {
        if (For(fact) is not null && !descriptor.EmittedFactCapabilities.Contains(GenerationFactCapability.MixedSlicePlacementFallback))
        {
            context.Add(AdapterContributionAdmissionDiagnosticCode.UndeclaredFactCapability, path, "Descriptor does not declare emitted fact capability 'MixedSlicePlacementFallback'", fact.Id, fact.Subject);
        }
    }

    static bool ValidName(string? name) => name is null || (!string.IsNullOrWhiteSpace(name) && name == name.Trim() && !name.Any(character => char.IsControl(character) || character is '/' or '\\'));
}
