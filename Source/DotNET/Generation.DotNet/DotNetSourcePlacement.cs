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
    /// Gets the fixed source structure for the artifact subject.
    /// </summary>
    public required DotNetSourceStructure Structure { get; init; }

    /// <summary>
    /// Gets the independently established semantic slice kind.
    /// </summary>
    public required GenerationSliceKind SliceKind { get; init; }

    /// <summary>
    /// Gets the host-owned source-structure policy for the owning project.
    /// </summary>
    public required DotNetSourceStructurePolicy Policy { get; init; }
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
    /// Gets the derived module, feature, slice, and slice-kind placement.
    /// </summary>
    public required ArtifactPlacement Placement { get; init; }
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
    /// Gets whether every distinct request produced an exact placement.
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

            if (request.Artifact.Subject != request.Structure.Subject)
            {
                diagnostics.Add(Diagnostic(
                    DotNetSourceStructureDiagnosticCodes.MismatchedPlacementSubject,
                    GenerationDiagnosticOutcome.Conflict,
                    request.Artifact.Subject,
                    $"Artifact subject '{request.Artifact.Subject.Value}' does not match source subject '{request.Structure.Subject.Value}'",
                    request.Structure.Source));
                continue;
            }

            var resolution = DotNetSourceStructureResolver.Resolve(request.Structure, request.SliceKind, request.Policy);
            diagnostics.AddRange(resolution.Diagnostics);
            if (resolution.Placement is not null)
            {
                placements.Add(new DotNetSourcePlacement
                {
                    Artifact = request.Artifact,
                    Structure = request.Structure,
                    Placement = resolution.Placement
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
        request.Structure.Namespace,
        string.Join('\u001e', request.Structure.ProjectRelativePaths.Order(StringComparer.Ordinal)),
        CanonicalSource(request.Structure.Source),
        ((int)request.SliceKind).ToString(CultureInfo.InvariantCulture),
        request.Policy.Version.ToString(CultureInfo.InvariantCulture),
        CanonicalOptional(request.Policy.FeatureRoot),
        request.Policy.NamespaceSegmentsToSkip.ToString(CultureInfo.InvariantCulture),
        CanonicalOptional(request.Policy.Module));

    static string CanonicalArtifact(ArtifactKey artifact) =>
        $"{artifact.Subject.Value}\u001f{((int)artifact.Kind).ToString(CultureInfo.InvariantCulture)}";

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
