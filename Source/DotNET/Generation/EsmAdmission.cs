// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines the independent ESM processing surfaces considered by Generation.
/// </summary>
public enum EsmSurface
{
    /// <summary>Reading a canonical ESM for processing.</summary>
    Reading = 0,

    /// <summary>Executing an ESM with a reference engine.</summary>
    ReferenceExecution = 1,

    /// <summary>Rendering an ESM into implementation artifacts.</summary>
    Rendering = 2,

    /// <summary>Recovering an ESM through Generation's bounded adapter behavior.</summary>
    ReverseRecovery = 3
}

/// <summary>
/// Defines the disposition of one ESM processing request. Only Admitted permits processing.
/// </summary>
public enum EsmDisposition
{
    /// <summary>The requested processing is refused.</summary>
    Refused = 0,

    /// <summary>The requested processing is admitted within the documented surface's scope.</summary>
    Admitted = 1,

    /// <summary>Generation has no implementation of the requested surface; this is not success.</summary>
    Inapplicable = 2
}

/// <summary>
/// Defines the machine-readable reason for an ESM admission decision.
/// </summary>
public enum EsmAdmissionReason
{
    /// <summary>No refusal applies to the admitted request.</summary>
    None = 0,

    /// <summary>The exact language and semantic version pair is not admitted.</summary>
    UnsupportedVersion = 1,

    /// <summary>Generation has no implementation of the requested surface.</summary>
    InapplicableSurface = 2,

    /// <summary>The requested surface is not recognized.</summary>
    UnknownSurface = 3,

    /// <summary>The bytes are not a valid canonical ESM contract.</summary>
    InvalidCanonicalInput = 4
}

/// <summary>
/// Represents an immutable, explicit decision about one ESM processing surface.
/// </summary>
public sealed record EsmAdmissionDecision
{
    internal EsmAdmissionDecision(
        EsmSurface surface,
        EsmDisposition disposition,
        EsmAdmissionReason reason,
        ImmutableArray<GenerationDiagnostic> diagnostics)
    {
        Surface = surface;
        Disposition = disposition;
        Reason = reason;
        Diagnostics = diagnostics;
    }

    /// <summary>Gets the requested processing surface.</summary>
    public EsmSurface Surface { get; }

    /// <summary>Gets whether processing is admitted, refused, or inapplicable.</summary>
    public EsmDisposition Disposition { get; }

    /// <summary>Gets the typed reason without requiring diagnostic-message parsing.</summary>
    public EsmAdmissionReason Reason { get; }

    /// <summary>Gets the blocking diagnostics, or an empty array for admission.</summary>
    public ImmutableArray<GenerationDiagnostic> Diagnostics { get; }
}

/// <summary>
/// Defines Generation's ESM admission policy independently of compiler or package support.
/// </summary>
public static class EsmAdmission
{
    // Retain the six exact legacy pairs to preserve existing bounded adapter behavior, not to
    // promise recovery of every construct. New versions require an explicit policy change.
    static readonly ImmutableArray<(LanguageVersion Language, SemanticVersion Semantic)> _legacyPairs =
    [
        (LanguageVersion.V1, SemanticVersion.V1),
        (LanguageVersion.V2, SemanticVersion.V2),
        (LanguageVersion.V3, SemanticVersion.V3),
        (LanguageVersion.V4, SemanticVersion.V4),
        (LanguageVersion.V5, SemanticVersion.V5),
        (LanguageVersion.V6, SemanticVersion.V6)
    ];

    /// <summary>
    /// Evaluates the exact ESM versions for one surface. Legacy reverse recovery retains only
    /// existing bounded adapter behavior and diagnostics, not support for every ESM construct.
    /// </summary>
    /// <param name="languageVersion">The ESM's actual language version.</param>
    /// <param name="semanticVersion">The ESM's actual semantic version.</param>
    /// <param name="surface">The intended processing surface.</param>
    /// <returns>An explicit decision; inapplicable and unknown surfaces are never admitted.</returns>
    public static EsmAdmissionDecision Evaluate(LanguageVersion languageVersion, SemanticVersion semanticVersion, EsmSurface surface)
    {
        var context = $"ESM language {languageVersion} / semantic {semanticVersion} for surface '{surface}'";
        return surface switch
        {
            EsmSurface.Reading or EsmSurface.ReverseRecovery => _legacyPairs.Contains((languageVersion, semanticVersion))
                ? new(surface, EsmDisposition.Admitted, EsmAdmissionReason.None, [])
                : NotAdmitted(surface, EsmDisposition.Refused, EsmAdmissionReason.UnsupportedVersion, $"{context} is not admitted by Generation."),
            EsmSurface.ReferenceExecution or EsmSurface.Rendering => NotAdmitted(
                surface, EsmDisposition.Inapplicable, EsmAdmissionReason.InapplicableSurface, $"{context} is inapplicable because Generation has no implementation of this surface."),
            _ => NotAdmitted(surface, EsmDisposition.Refused, EsmAdmissionReason.UnknownSurface, $"{context} is refused because the surface is unknown.")
        };
    }

    static EsmAdmissionDecision NotAdmitted(EsmSurface surface, EsmDisposition disposition, EsmAdmissionReason reason, string message) => new(
        surface,
        disposition,
        reason,
        [
            new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.EsmProcessingNotAdmitted,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = $"{message} See https://github.com/Cratis/Screenplay.Generation/issues/71."
            }
        ]);
}
