// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Specifies an explicit omission coalescing policy.</summary>
public enum OccurrenceOmissionDiagnosticPolicy
{
    /// <summary>No policy was selected.</summary>
    Unknown = 0,

    /// <summary>Retain one representative per compatible family and exact occurrence.</summary>
    OnePerOccurrence = 1
}

/// <summary>Describes the observed evidence, not the authority of its producer.</summary>
public enum OmissionDiagnosticEvidencePreference
{
    /// <summary>No evidence preference was selected.</summary>
    Unknown = 0,

    /// <summary>An ordinary use of the artifact.</summary>
    UseSite = 1,

    /// <summary>An explicit artifact registration.</summary>
    ExplicitRegistration = 2,

    /// <summary>Explicit configuration of the artifact identity.</summary>
    IdentityConfiguration = 3
}

/// <summary>Identifies an agreed omission family independently from any producer or message.</summary>
public sealed record DiagnosticFamilyId
{
    /// <summary>Gets the normalized, stable family identity.</summary>
    public required string Value { get; init; }
}

/// <summary>Requests deterministic coalescing without replacing ordinary diagnostics.</summary>
public sealed record OccurrenceOmissionDiagnosticDefinition
{
    /// <summary>Gets the shared semantic family.</summary>
    public required DiagnosticFamilyId Family { get; init; }

    /// <summary>Gets the exact subject, role and nullable backend scope.</summary>
    public required ArtifactKey Occurrence { get; init; }

    /// <summary>Gets the explicitly selected coalescing policy.</summary>
    public required OccurrenceOmissionDiagnosticPolicy Policy { get; init; }

    /// <summary>Gets the observed evidence preference.</summary>
    public required OmissionDiagnosticEvidencePreference Preference { get; init; }

    /// <summary>Gets the located payload with nonempty same-producer supporting facts.</summary>
    public required GenerationDiagnostic Diagnostic { get; init; }
}

/// <summary>An opt-in omission candidate; an optional local support facet controls method eligibility.</summary>
public sealed record OccurrenceOmissionDiagnosticFact : GenerationFact
{
    /// <summary>Gets the candidate metadata and original producer-local payload.</summary>
    public required OccurrenceOmissionDiagnosticDefinition Definition { get; init; }
}
