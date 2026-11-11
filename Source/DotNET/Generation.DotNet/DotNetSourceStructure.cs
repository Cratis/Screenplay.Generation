// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Defines the role one analyzed project has in the application source set.
/// </summary>
public enum DotNetProjectRole
{
    /// <summary>
    /// The host did not declare a supported project role.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The project contains application artifacts.
    /// </summary>
    Application = 0,

    /// <summary>
    /// The project contains executable specifications for application artifacts.
    /// </summary>
    Specifications = 1
}

/// <summary>
/// Defines the host-owned policy for deriving module, feature, and slice placement from .NET source structure.
/// </summary>
public sealed record DotNetSourceStructurePolicy
{
    /// <summary>
    /// Gets the source-structure policy version.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Gets an optional project-relative directory beneath which module, feature, and slice folders begin.
    /// </summary>
    public string? FeatureRoot { get; init; }

    /// <summary>
    /// Gets the number of leading namespace segments omitted before deriving placement.
    /// </summary>
    public int NamespaceSegmentsToSkip { get; init; }

    /// <summary>
    /// Gets an optional module that collapses every source root into one module.
    /// </summary>
    public string? Module { get; init; }
}

/// <summary>
/// Describes the fixed source-structure evidence for one source subject.
/// </summary>
public sealed record DotNetSourceStructure
{
    /// <summary>
    /// Gets the source subject whose placement is being derived.
    /// </summary>
    public required SubjectId Subject { get; init; }

    /// <summary>
    /// Gets the stable identity of the project containing the source subject.
    /// </summary>
    public required string Project { get; init; }

    /// <summary>
    /// Gets the role of the project containing the source subject.
    /// </summary>
    public required DotNetProjectRole ProjectRole { get; init; }

    /// <summary>
    /// Gets the declared namespace of the source subject.
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// Gets every authored source file path relative to the owning project.
    /// </summary>
    public IReadOnlyList<string> ProjectRelativePaths { get; init; } = [];

    /// <summary>
    /// Gets the exact source range establishing the source subject.
    /// </summary>
    public SourceRange? Source { get; init; }
}

/// <summary>
/// Represents one source-structure placement derivation and its typed diagnostics.
/// </summary>
public sealed record DotNetSourceStructureResolution
{
    /// <summary>
    /// Gets the source structure used for the derivation.
    /// </summary>
    public required DotNetSourceStructure Structure { get; init; }

    /// <summary>
    /// Gets the derived placement when every available source structure agrees.
    /// </summary>
    public ArtifactPlacement? Placement { get; init; }

    /// <summary>
    /// Gets the diagnostics that prevented exact placement.
    /// </summary>
    public IReadOnlyList<GenerationDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// Gets whether an exact placement was derived.
    /// </summary>
    public bool IsSuccess => Placement is not null && Diagnostics.Count == 0;
}

/// <summary>
/// Defines stable diagnostic codes emitted by .NET source-structure derivation.
/// </summary>
public static class DotNetSourceStructureDiagnosticCodes
{
    /// <summary>
    /// The host supplied an unsupported source-structure policy.
    /// </summary>
    public const string UnsupportedPolicy = "DOTNETSP0001";

    /// <summary>
    /// A source or feature-root path is malformed, rooted, or traversing.
    /// </summary>
    public const string InvalidPath = "DOTNETSP0002";

    /// <summary>
    /// A source file is not beneath the configured feature root.
    /// </summary>
    public const string MissingFeatureRoot = "DOTNETSP0003";

    /// <summary>
    /// The available source structure cannot identify both a module and a slice.
    /// </summary>
    public const string InsufficientStructure = "DOTNETSP0004";

    /// <summary>
    /// Folder and namespace structures assert different placements.
    /// </summary>
    public const string ConflictingStructure = "DOTNETSP0005";

    /// <summary>
    /// The host supplied an unsupported project role.
    /// </summary>
    public const string UnsupportedProjectRole = "DOTNETSP0006";

    /// <summary>
    /// The caller supplied an unsupported semantic slice kind.
    /// </summary>
    public const string UnsupportedSliceKind = "DOTNETSP0007";

    /// <summary>
    /// The declared namespace is malformed.
    /// </summary>
    public const string InvalidNamespace = "DOTNETSP0008";

    /// <summary>
    /// An analyzed project has no host-supplied source context.
    /// </summary>
    public const string MissingSourceContext = "DOTNETSP0009";

    /// <summary>
    /// An authored declaration is absent from the host-supplied source context.
    /// </summary>
    public const string MissingSourceMapping = "DOTNETSP0010";

    /// <summary>
    /// More than one analyzed project declares the same source subject.
    /// </summary>
    public const string DuplicateSourceSubject = "DOTNETSP0011";

    /// <summary>
    /// An artifact role and its source structure identify different subjects.
    /// </summary>
    public const string MismatchedPlacementSubject = "DOTNETSP0012";

    /// <summary>
    /// More than one distinct placement request targets the same artifact role.
    /// </summary>
    public const string ConflictingPlacementRequests = "DOTNETSP0013";

    /// <summary>
    /// A source placement request identifies an unknown or undefined artifact role.
    /// </summary>
    public const string UnsupportedPlacementArtifactKind = "DOTNETSP0014";

    /// <summary>
    /// An explicit compatibility placement policy is malformed or unsupported.
    /// </summary>
    public const string UnsupportedCompatibilityPolicy = "DOTNETSP0015";
}

/// <summary>
/// Derives one deterministic module, feature, and slice placement from fixed .NET source evidence.
/// </summary>
public static class DotNetSourceStructureResolver
{
    /// <summary>
    /// Derives placement from the supplied fixed source structure and host policy.
    /// </summary>
    /// <param name="structure">The fixed source structure.</param>
    /// <param name="sliceKind">The independently established semantic slice kind.</param>
    /// <param name="policy">The host-owned source-structure policy.</param>
    /// <returns>The exact placement or typed blocking diagnostics.</returns>
    public static DotNetSourceStructureResolution Resolve(
        DotNetSourceStructure structure,
        GenerationSliceKind sliceKind,
        DotNetSourceStructurePolicy policy)
    {
        var unsupported = UnsupportedInput(structure, sliceKind, policy);
        if (unsupported is not null)
        {
            return unsupported;
        }

        string[] featureRoot = [];
        if (policy.FeatureRoot is not null && !TryNormalizeRelativePath(policy.FeatureRoot, out featureRoot))
        {
            return Failed(
                structure,
                DotNetSourceStructureDiagnosticCodes.InvalidPath,
                $"The project-relative feature root '{policy.FeatureRoot}' is invalid");
        }

        var scan = ScanFolders(structure, sliceKind, policy, featureRoot);
        if (scan.Failure is not null)
        {
            return scan.Failure;
        }

        var folderPlacements = scan.Placements;
        if (folderPlacements.Count > 1)
        {
            return Failed(
                structure,
                DotNetSourceStructureDiagnosticCodes.ConflictingStructure,
                $"Authored source folders assert conflicting placements: {string.Join(", ", folderPlacements.Select(Describe).Order(StringComparer.Ordinal))}");
        }

        var folderPlacement = folderPlacements.SingleOrDefault();
        if (!TryNamespaceSegments(structure.Namespace, out var declaredNamespaceSegments))
        {
            return Failed(
                structure,
                DotNetSourceStructureDiagnosticCodes.InvalidNamespace,
                $"The declared namespace '{structure.Namespace}' is invalid");
        }

        var namespaceSegments = declaredNamespaceSegments.Skip(policy.NamespaceSegmentsToSkip).ToArray();
        var namespacePlacement = PlacementFrom(namespaceSegments, sliceKind, policy.Module);
        return Merge(structure, folderPlacement, namespacePlacement);
    }

    static DotNetSourceStructureResolution? UnsupportedInput(
        DotNetSourceStructure structure,
        GenerationSliceKind sliceKind,
        DotNetSourceStructurePolicy policy)
    {
        if (!IsSupportedPolicy(policy))
        {
            return Failed(
                structure,
                DotNetSourceStructureDiagnosticCodes.UnsupportedPolicy,
                "The .NET source-structure policy is unsupported");
        }

        if (structure.ProjectRole is not DotNetProjectRole.Application and not DotNetProjectRole.Specifications)
        {
            return Failed(
                structure,
                DotNetSourceStructureDiagnosticCodes.UnsupportedProjectRole,
                $"The .NET project role '{structure.ProjectRole}' is unsupported");
        }

        return IsSupportedSliceKind(sliceKind)
            ? null
            : Failed(
                structure,
                DotNetSourceStructureDiagnosticCodes.UnsupportedSliceKind,
                $"The semantic slice kind '{sliceKind}' is unsupported");
    }

    static bool IsSupportedPolicy(DotNetSourceStructurePolicy policy) =>
        policy.Version == 1 &&
        policy.NamespaceSegmentsToSkip >= 0 &&
        (policy.Module is null || IsName(policy.Module));

    static bool IsSupportedSliceKind(GenerationSliceKind sliceKind) =>
        sliceKind is GenerationSliceKind.StateChange or
            GenerationSliceKind.StateView or
            GenerationSliceKind.Automation or
            GenerationSliceKind.Translate;

    static FolderScan ScanFolders(
        DotNetSourceStructure structure,
        GenerationSliceKind sliceKind,
        DotNetSourceStructurePolicy policy,
        string[] featureRoot)
    {
        List<ArtifactPlacement> folderPlacements = [];
        foreach (var path in structure.ProjectRelativePaths.Order(StringComparer.Ordinal))
        {
            if (!TryNormalizeRelativePath(path, out var sourceSegments))
            {
                return new(
                    Failed(
                        structure,
                        DotNetSourceStructureDiagnosticCodes.InvalidPath,
                        $"The project-relative source path '{path}' is invalid"),
                    folderPlacements);
            }

            if (!TryStripFeatureRoot(sourceSegments[..^1], featureRoot, out var directorySegments))
            {
                return new(
                    Failed(
                        structure,
                        DotNetSourceStructureDiagnosticCodes.MissingFeatureRoot,
                        $"The source path '{path}' is not beneath feature root '{policy.FeatureRoot}'"),
                    folderPlacements);
            }

            if (PlacementFrom(directorySegments, sliceKind, policy.Module) is { } candidate &&
                !folderPlacements.Exists(existing => SamePlacement(existing, candidate)))
            {
                folderPlacements.Add(candidate);
            }
        }

        return new(null, folderPlacements);
    }

    static bool TryStripFeatureRoot(string[] directorySegments, string[] featureRoot, out string[] remaining)
    {
        remaining = directorySegments;
        if (featureRoot.Length == 0)
        {
            return true;
        }

        if (!StartsWith(directorySegments, featureRoot))
        {
            return false;
        }

        remaining = directorySegments[featureRoot.Length..];
        return true;
    }

    static DotNetSourceStructureResolution Merge(
        DotNetSourceStructure structure,
        ArtifactPlacement? folderPlacement,
        ArtifactPlacement? namespacePlacement)
    {
        if (AreConflicting(folderPlacement, namespacePlacement))
        {
            return Failed(
                structure,
                DotNetSourceStructureDiagnosticCodes.ConflictingStructure,
                $"Folder placement '{Describe(folderPlacement!)}' conflicts with namespace placement '{Describe(namespacePlacement!)}'");
        }

        var placement = folderPlacement ?? namespacePlacement;
        if (placement is null)
        {
            return Failed(
                structure,
                DotNetSourceStructureDiagnosticCodes.InsufficientStructure,
                "The source folder and namespace do not identify both a module and a slice");
        }

        return new()
        {
            Structure = structure,
            Placement = placement
        };
    }

    static DotNetSourceStructureResolution Failed(
        DotNetSourceStructure structure,
        string code,
        string message) => new()
        {
            Structure = structure,
            Diagnostics =
            [
                new GenerationDiagnostic
                {
                    Code = code,
                    Severity = GenerationDiagnosticSeverity.Error,
                    Outcome = GenerationDiagnosticOutcome.Unsupported,
                    Message = message,
                    Source = structure.Source,
                    Subject = structure.Subject
                }
            ]
        };

    static ArtifactPlacement? PlacementFrom(
        string[] sourceSegments,
        GenerationSliceKind sliceKind,
        string? configuredModule)
    {
        var segments = sourceSegments.Where(IsName).ToArray();
        if (segments.Length != sourceSegments.Length)
        {
            return null;
        }

        if (configuredModule is null)
        {
            return segments.Length < 2 ? null : Place(segments[0], segments[1..], sliceKind);
        }

        var remaining = segments.Length > 0 && string.Equals(segments[0], configuredModule, StringComparison.Ordinal)
            ? segments[1..]
            : segments;
        return remaining.Length == 0 ? null : Place(configuredModule, remaining, sliceKind);
    }

    static ArtifactPlacement Place(string module, string[] featuresAndSlice, GenerationSliceKind sliceKind) => new()
    {
        Module = module,
        Features = featuresAndSlice.Length == 1 ? [] : featuresAndSlice[..^1],
        Slice = featuresAndSlice[^1],
        SliceKind = sliceKind
    };

    static bool TryNormalizeRelativePath(string value, out string[] segments)
    {
        segments = [];
        if (!TryNormalize(value, out var normalized))
        {
            return false;
        }

        var folded = normalized.Replace('\\', '/');
        if (IsUnusablePath(folded))
        {
            return false;
        }

        var parts = folded.Split('/').Where(part => !string.IsNullOrEmpty(part) && part != ".").ToArray();
        if (parts.Length == 0 || !parts.All(IsName))
        {
            return false;
        }

        segments = parts;
        return true;
    }

    static bool TryNormalize(string value, out string normalized)
    {
        normalized = string.Empty;
        try
        {
            normalized = value.Normalize();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    static bool IsUnusablePath(string normalized) =>
        string.IsNullOrWhiteSpace(normalized) ||
        normalized.Any(char.IsControl) ||
        normalized[0] == '/' ||
        (normalized.Length >= 2 && char.IsLetter(normalized[0]) && normalized[1] == ':');

    static bool TryNamespaceSegments(string value, out string[] segments)
    {
        segments = [];
        if (!TryNormalize(value, out var normalized))
        {
            return false;
        }

        if (string.IsNullOrEmpty(normalized))
        {
            return true;
        }

        if (normalized.Any(char.IsControl) || HasEmptyDottedSegment(normalized))
        {
            return false;
        }

        segments = normalized.Split('.');
        return segments.All(IsName);
    }

    static bool HasEmptyDottedSegment(string normalized) =>
        normalized[0] == '.' ||
        normalized[^1] == '.' ||
        normalized.Contains("..", StringComparison.Ordinal);

    static bool StartsWith(string[] source, string[] prefix) =>
        source.Length >= prefix.Length &&
        source.Take(prefix.Length).SequenceEqual(prefix, StringComparer.Ordinal);

    static bool SamePlacement(ArtifactPlacement first, ArtifactPlacement second) =>
        string.Equals(first.Module, second.Module, StringComparison.Ordinal) &&
        first.Features.SequenceEqual(second.Features, StringComparer.Ordinal) &&
        string.Equals(first.Slice, second.Slice, StringComparison.Ordinal) &&
        first.SliceKind == second.SliceKind;

    static bool IsName(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        string.Equals(value, value.Trim(), StringComparison.Ordinal) &&
        !value.Any(char.IsControl) &&
        value is not "." and not "..";

    static string Describe(ArtifactPlacement placement) =>
        string.Join('/', new[] { placement.Module }.Concat(placement.Features).Append(placement.Slice));

    static bool AreConflicting(ArtifactPlacement? folderPlacement, ArtifactPlacement? namespacePlacement) =>
        folderPlacement is not null &&
        namespacePlacement is not null &&
        !SamePlacement(folderPlacement, namespacePlacement);

    sealed record FolderScan(DotNetSourceStructureResolution? Failure, List<ArtifactPlacement> Placements);
}
