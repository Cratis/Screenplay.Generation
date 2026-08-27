// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines a deterministic structural contribution-admission failure.
/// </summary>
public enum AdapterContributionAdmissionDiagnosticCode
{
    /// <summary>
    /// The admission failure is unknown.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A required value is absent or blank.
    /// </summary>
    MissingRequiredValue = 0,

    /// <summary>
    /// A required collection is unexpectedly null.
    /// </summary>
    NullRequiredCollection = 1,

    /// <summary>
    /// An enum contains its explicit unknown value.
    /// </summary>
    UnknownEnumValue = 2,

    /// <summary>
    /// An enum contains an undefined numeric value.
    /// </summary>
    UndefinedEnumValue = 3,

    /// <summary>
    /// The adapter descriptor identity is malformed.
    /// </summary>
    InvalidDescriptorIdentity = 4,

    /// <summary>
    /// The compatible Generation version range is malformed.
    /// </summary>
    InvalidGenerationVersionRange = 5,

    /// <summary>
    /// The contribution producer does not equal the descriptor identity.
    /// </summary>
    ContributionAdapterMismatch = 6,

    /// <summary>
    /// Fact evidence names a producer other than the descriptor identity.
    /// </summary>
    EvidenceAdapterMismatch = 7,

    /// <summary>
    /// A fact identity is empty or not normalized.
    /// </summary>
    InvalidFactId = 8,

    /// <summary>
    /// A fact identity is not scoped beneath the producing adapter identity.
    /// </summary>
    UnscopedFactId = 9,

    /// <summary>
    /// A fact identity occurs more than once in the contribution.
    /// </summary>
    DuplicateFactId = 10,

    /// <summary>
    /// A subject is not a normalized absolute stable URI.
    /// </summary>
    InvalidSubject = 11,

    /// <summary>
    /// The descriptor does not declare the emitted fact family.
    /// </summary>
    UndeclaredFactCapability = 12,

    /// <summary>
    /// The contribution contains a fact family not defined by the neutral contracts.
    /// </summary>
    UnsupportedFactType = 13,

    /// <summary>
    /// A nested definition identifies a different owner from its containing fact or chain.
    /// </summary>
    OwnershipMismatch = 14,

    /// <summary>
    /// A fact discriminator does not carry the operand required by its kind.
    /// </summary>
    InvalidKindOperand = 15,

    /// <summary>
    /// A source range is structurally malformed.
    /// </summary>
    InvalidSourceRange = 16,

    /// <summary>
    /// The source host rejected a range as nonauthoritative.
    /// </summary>
    SourceNotAuthoritative = 17,

    /// <summary>
    /// An adapter diagnostic is structurally malformed.
    /// </summary>
    InvalidContributionDiagnostic = 18,

    /// <summary>
    /// Source evidence was supplied without a host authority validator.
    /// </summary>
    SourceAuthorityRequired = 19,

    /// <summary>
    /// A required API capability identity is malformed.
    /// </summary>
    InvalidApiCapability = 20,

    /// <summary>
    /// A required API capability occurs more than once.
    /// </summary>
    DuplicateApiCapability = 21
}

/// <summary>
/// Describes one deterministic contribution-admission diagnostic.
/// </summary>
public sealed record AdapterContributionAdmissionDiagnostic
{
    /// <summary>
    /// Gets the typed diagnostic code.
    /// </summary>
    public required AdapterContributionAdmissionDiagnosticCode Code { get; init; }

    /// <summary>
    /// Gets the stable contract path identifying the malformed value.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the human-readable diagnostic message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the affected fact identity, when available.
    /// </summary>
    public FactId? Fact { get; init; }

    /// <summary>
    /// Gets the affected subject, when available.
    /// </summary>
    public SubjectId? Subject { get; init; }

    /// <summary>
    /// Gets the affected source range, when available.
    /// </summary>
    public SourceRange? Source { get; init; }
}

/// <summary>
/// Represents the nonthrowing atomic result of contribution admission.
/// </summary>
public sealed record AdapterContributionAdmissionResult
{
    /// <summary>
    /// Gets the admitted immutable snapshot, or <see langword="null"/> when any diagnostic rejected the contribution.
    /// </summary>
    public AdapterContributionSnapshot? Snapshot { get; init; }

    /// <summary>
    /// Gets the deterministic admission diagnostics.
    /// </summary>
    public ImmutableArray<AdapterContributionAdmissionDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// Gets whether the contribution was admitted atomically.
    /// </summary>
    public bool IsAdmitted => Snapshot is not null && Diagnostics.IsEmpty;
}
