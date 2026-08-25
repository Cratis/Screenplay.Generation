// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Requests one artifact placement from fixed source structure and an independently established slice kind.
/// </summary>
public sealed record DotNetSourcePlacementRequest
{
    /// <summary>
    /// Gets the exact artifact role to place.
    /// </summary>
    public required ArtifactKey Artifact { get; init; }

    /// <summary>
    /// Gets the fixed source structure used to place the artifact.
    /// </summary>
    public required DotNetSourceStructure Structure { get; init; }

    /// <summary>
    /// Gets the exact alternate source subject that owns the artifact's source placement.
    /// </summary>
    /// <remarks>
    /// Leave this value absent when the artifact owns its source structure. Method-backed and synthetic artifacts
    /// can nominate their exact containing type or model without changing the fixed source snapshot.
    /// </remarks>
    public SubjectId? SourceOwner { get; init; }

    /// <summary>
    /// Gets the independently established semantic slice kind.
    /// </summary>
    public required GenerationSliceKind SliceKind { get; init; }

    /// <summary>
    /// Gets the host-owned source-structure policy for the owning project.
    /// </summary>
    public required DotNetSourceStructurePolicy Policy { get; init; }

    /// <summary>
    /// Gets the optional explicit compatibility policy used only when strict source derivation reports insufficient structure.
    /// </summary>
    public DotNetSourcePlacementCompatibilityPolicy? CompatibilityPolicy { get; init; }
}

/// <summary>
/// Defines one explicit compatibility placement for source structures that cannot identify both a module and a slice.
/// </summary>
public sealed record DotNetSourcePlacementCompatibilityPolicy
{
    /// <summary>
    /// Gets the compatibility policy version.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Gets the exact compatibility placement supplied by the adapter.
    /// </summary>
    public required ArtifactPlacement Placement { get; init; }
}

/// <summary>
/// Represents one artifact placement derived from fixed source structure.
/// </summary>
public sealed record DotNetSourcePlacement
{
    /// <summary>
    /// Gets the exact artifact role being placed.
    /// </summary>
    public required ArtifactKey Artifact { get; init; }

    /// <summary>
    /// Gets the fixed source structure used for the derivation.
    /// </summary>
    public required DotNetSourceStructure Structure { get; init; }

    /// <summary>
    /// Gets the exact alternate source-placement owner nominated by the request, if any.
    /// </summary>
    public SubjectId? SourceOwner { get; init; }

    /// <summary>
    /// Gets the strict source-structure policy used for the derivation.
    /// </summary>
    public DotNetSourceStructurePolicy Policy { get; init; } = new();

    /// <summary>
    /// Gets the explicit compatibility policy requested for the derivation, if any.
    /// </summary>
    public DotNetSourcePlacementCompatibilityPolicy? CompatibilityPolicy { get; init; }

    /// <summary>
    /// Gets the derived module, feature, slice, and slice-kind placement.
    /// </summary>
    public required ArtifactPlacement Placement { get; init; }

    /// <summary>
    /// Gets whether strict derivation failed solely with <c>DOTNETSP0004</c> and the explicit compatibility placement was used.
    /// </summary>
    public bool UsedCompatibilityPlacement { get; init; }

    /// <summary>
    /// Gets the strict diagnostic code that authorized compatibility placement, if compatibility was used.
    /// </summary>
    public string? CompatibilityReasonCode { get; init; }
}

/// <summary>
/// Represents the deterministic result of one fixed-snapshot source placement derivation.
/// </summary>
public sealed record DotNetSourcePlacementSnapshot
{
    /// <summary>
    /// Gets derived placements in canonical artifact order.
    /// </summary>
    public IReadOnlyList<DotNetSourcePlacement> Placements { get; init; } = [];

    /// <summary>
    /// Gets typed diagnostics that blocked exact placement.
    /// </summary>
    public IReadOnlyList<GenerationDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// Gets whether every distinct request produced a strict or explicitly authorized compatibility placement.
    /// </summary>
    public bool IsSuccess => Diagnostics.Count == 0;
}

/// <summary>
/// Resolves independently contributed artifact roles against one fixed .NET source-structure snapshot.
/// </summary>
public static class DotNetSourcePlacementDerivation
{
    /// <summary>
    /// Derives placements independently from request enumeration order.
    /// </summary>
    /// <param name="requests">The artifact placement requests.</param>
    /// <returns>The canonical placements and typed diagnostics.</returns>
    public static DotNetSourcePlacementSnapshot Derive(IEnumerable<DotNetSourcePlacementRequest> requests)
    {
        var canonicalRequests = requests
            .GroupBy(CanonicalRequest, StringComparer.Ordinal)
            .OrderBy(_ => _.Key, StringComparer.Ordinal)
            .Select(_ => _.First())
            .ToArray();
        var placements = new List<DotNetSourcePlacement>();
        var diagnostics = new List<GenerationDiagnostic>();

        foreach (var group in canonicalRequests
                     .GroupBy(_ => CanonicalArtifact(_.Artifact), StringComparer.Ordinal)
                     .OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            var artifactRequests = group.ToArray();
            var artifact = artifactRequests[0].Artifact;
            if (artifactRequests.Length > 1)
            {
                diagnostics.Add(Diagnostic(
                    DotNetSourceStructureDiagnosticCodes.ConflictingPlacementRequests,
                    GenerationDiagnosticOutcome.Conflict,
                    artifact.Subject,
                    $"Artifact '{group.Key}' has conflicting source-placement requests"));
                continue;
            }

            var request = artifactRequests[0];
            if (!Enum.IsDefined(request.Artifact.Kind) || request.Artifact.Kind == ArtifactKind.Unknown)
            {
                diagnostics.Add(Diagnostic(
                    DotNetSourceStructureDiagnosticCodes.UnsupportedPlacementArtifactKind,
                    GenerationDiagnosticOutcome.Unknown,
                    request.Artifact.Subject,
                    $"Artifact kind '{request.Artifact.Kind}' cannot request source placement",
                    request.Structure.Source));
                continue;
            }

            var sourceOwner = request.SourceOwner ?? request.Artifact.Subject;
            if ((request.SourceOwner is not null &&
                 (request.SourceOwner == request.Artifact.Subject || !IsSubject(request.SourceOwner))) ||
                sourceOwner != request.Structure.Subject)
            {
                var message = request.SourceOwner is null
                    ? $"Artifact subject '{request.Artifact.Subject.Value}' does not match source subject '{request.Structure.Subject.Value}'"
                    : $"Artifact subject '{request.Artifact.Subject.Value}' nominates invalid or mismatched source owner '{request.SourceOwner.Value}' for source subject '{request.Structure.Subject.Value}'";
                diagnostics.Add(Diagnostic(
                    DotNetSourceStructureDiagnosticCodes.MismatchedPlacementSubject,
                    GenerationDiagnosticOutcome.Conflict,
                    request.Artifact.Subject,
                    message,
                    request.Structure.Source));
                continue;
            }

            var resolution = DotNetSourceStructureResolver.Resolve(request.Structure, request.SliceKind, request.Policy);
            var insufficientStructure = resolution.Placement is null &&
                                        resolution.Diagnostics.Count == 1 &&
                                        resolution.Diagnostics[0].Code == DotNetSourceStructureDiagnosticCodes.InsufficientStructure;
            if (!resolution.IsSuccess && !insufficientStructure)
            {
                diagnostics.AddRange(resolution.Diagnostics);
                continue;
            }

            if (!IsSupported(request.CompatibilityPolicy, request.SliceKind))
            {
                diagnostics.Add(Diagnostic(
                    DotNetSourceStructureDiagnosticCodes.UnsupportedCompatibilityPolicy,
                    GenerationDiagnosticOutcome.Unsupported,
                    request.Artifact.Subject,
                    "The explicit .NET source-placement compatibility policy is unsupported",
                    request.Structure.Source));
                continue;
            }

            var compatibilityUsed = insufficientStructure && request.CompatibilityPolicy is not null;
            if (insufficientStructure && !compatibilityUsed)
            {
                diagnostics.AddRange(resolution.Diagnostics);
            }

            var compatibilityPolicy = Snapshot(request.CompatibilityPolicy);
            var placement = resolution.Placement ?? (compatibilityUsed ? compatibilityPolicy!.Placement : null);
            if (placement is not null)
            {
                placements.Add(new DotNetSourcePlacement
                {
                    Artifact = request.Artifact,
                    Structure = request.Structure,
                    SourceOwner = request.SourceOwner,
                    Policy = request.Policy with { },
                    CompatibilityPolicy = compatibilityPolicy,
                    Placement = placement,
                    UsedCompatibilityPlacement = compatibilityUsed,
                    CompatibilityReasonCode = compatibilityUsed
                        ? DotNetSourceStructureDiagnosticCodes.InsufficientStructure
                        : null
                });
            }
        }

        return new()
        {
            Placements =
            [
                .. placements.OrderBy(_ => CanonicalArtifact(_.Artifact), StringComparer.Ordinal)
            ],
            Diagnostics =
            [
                .. diagnostics
                    .OrderBy(_ => _.Code, StringComparer.Ordinal)
                    .ThenBy(_ => _.Subject?.Value, StringComparer.Ordinal)
                    .ThenBy(_ => _.Message, StringComparer.Ordinal)
            ]
        };
    }

    static GenerationDiagnostic Diagnostic(
        string code,
        GenerationDiagnosticOutcome outcome,
        SubjectId subject,
        string message,
        SourceRange? source = null) => new()
        {
            Code = code,
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = outcome,
            Message = message,
            Source = source,
            Subject = subject
        };

    static string CanonicalRequest(DotNetSourcePlacementRequest request) => string.Join(
        '\u001f',
        CanonicalArtifact(request.Artifact),
        request.Structure.Project,
        ((int)request.Structure.ProjectRole).ToString(CultureInfo.InvariantCulture),
        request.Structure.Subject.Value,
        CanonicalSubject(request.SourceOwner),
        request.Structure.Namespace,
        string.Join('\u001e', request.Structure.ProjectRelativePaths.Order(StringComparer.Ordinal)),
        CanonicalSource(request.Structure.Source),
        ((int)request.SliceKind).ToString(CultureInfo.InvariantCulture),
        request.Policy.Version.ToString(CultureInfo.InvariantCulture),
        CanonicalOptional(request.Policy.FeatureRoot),
        request.Policy.NamespaceSegmentsToSkip.ToString(CultureInfo.InvariantCulture),
        CanonicalOptional(request.Policy.Module),
        CanonicalCompatibilityPolicy(request.CompatibilityPolicy));

    static string CanonicalArtifact(ArtifactKey artifact) =>
        $"{artifact.Subject.Value}\u001f{((int)artifact.Kind).ToString(CultureInfo.InvariantCulture)}";

    static string CanonicalSubject(SubjectId? subject) => subject is null ? "0" : $"1{subject.Value}";

    static string CanonicalCompatibilityPolicy(DotNetSourcePlacementCompatibilityPolicy? policy)
    {
        if (policy is null)
        {
            return "0";
        }

        if (policy.Placement is null)
        {
            return $"1{policy.Version.ToString(CultureInfo.InvariantCulture)}\u001e0";
        }

        return $"1{string.Join(
            '\u001e',
            policy.Version.ToString(CultureInfo.InvariantCulture),
            policy.Placement.Module,
            policy.Placement.Features is null ? "0" : $"1{string.Join('\u001d', policy.Placement.Features)}",
            policy.Placement.Slice,
            ((int)policy.Placement.SliceKind).ToString(CultureInfo.InvariantCulture))}";
    }

    static DotNetSourcePlacementCompatibilityPolicy? Snapshot(DotNetSourcePlacementCompatibilityPolicy? policy) => policy is null
        ? null
        : policy with
        {
            Placement = policy.Placement with
            {
                Features = [.. policy.Placement.Features]
            }
        };

    static bool IsSupported(
        DotNetSourcePlacementCompatibilityPolicy? policy,
        GenerationSliceKind sliceKind) => policy is null ||
        (policy.Version == 1 &&
         policy.Placement is not null &&
         IsName(policy.Placement.Module) &&
         policy.Placement.Features?.All(IsName) == true &&
         IsName(policy.Placement.Slice) &&
         policy.Placement.SliceKind == sliceKind &&
         sliceKind is GenerationSliceKind.StateChange or
             GenerationSliceKind.StateView or
             GenerationSliceKind.Automation or
             GenerationSliceKind.Translate);

    static bool IsSubject(SubjectId subject) =>
        !string.IsNullOrWhiteSpace(subject.Value) &&
        string.Equals(subject.Value, subject.Value.Trim(), StringComparison.Ordinal) &&
        !subject.Value.Any(char.IsControl);

    static bool IsName(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        string.Equals(value, value.Trim(), StringComparison.Ordinal) &&
        !value.Any(char.IsControl) &&
        !value.Contains('/') &&
        !value.Contains('\\') &&
        value is not "." and not "..";

    static string CanonicalSource(SourceRange? source) => source is null
        ? "0"
        : $"1{string.Join(
            '\u001e',
            source.Path,
            CanonicalFileIdentity(source.FileIdentity),
            source.StartLine.ToString(CultureInfo.InvariantCulture),
            source.StartColumn.ToString(CultureInfo.InvariantCulture),
            source.EndLine.ToString(CultureInfo.InvariantCulture),
            source.EndColumn.ToString(CultureInfo.InvariantCulture))}";

    static string CanonicalFileIdentity(SourceFileIdentity? identity) => identity is null
        ? "0"
        : $"1{identity.Project}\u001d{identity.Path}";

    static string CanonicalOptional(string? value) => value is null ? "0" : $"1{value}";
}
