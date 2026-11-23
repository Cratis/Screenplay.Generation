// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>Maps selected owners through fixed .NET source structures and the existing strict placement rules.</summary>
public sealed class DotNetArtifactSourcePlacementResolver : IArtifactSourcePlacementResolver
{
    readonly DotNetSourceStructureSnapshot _snapshot;
    readonly Dictionary<string, DotNetSourceStructurePolicy> _policies;
    readonly IReadOnlyDictionary<ArtifactKey, DotNetSourcePlacementCompatibilityPolicy> _compatibility;

    /// <summary>Copies host-owned structures and policies once; resolution performs no discovery or I/O.</summary>
    /// <param name="snapshot">The fixed source structures.</param>
    /// <param name="policies">Strict policies by exact project identity.</param>
    /// <param name="compatibility">Optional explicit compatibility policies by exact target artifact.</param>
    public DotNetArtifactSourcePlacementResolver(DotNetSourceStructureSnapshot snapshot, IReadOnlyDictionary<string, DotNetSourceStructurePolicy> policies, IReadOnlyDictionary<ArtifactKey, DotNetSourcePlacementCompatibilityPolicy>? compatibility = null)
    {
        _snapshot = snapshot with { Structures = [.. snapshot.Structures.Select(structure => structure with { ProjectRelativePaths = [.. structure.ProjectRelativePaths] })], Diagnostics = [.. snapshot.Diagnostics] };
        _policies = policies.ToDictionary(pair => pair.Key, pair => pair.Value with { }, StringComparer.Ordinal);
        _compatibility = (compatibility ?? new Dictionary<ArtifactKey, DotNetSourcePlacementCompatibilityPolicy>()).ToDictionary(pair => pair.Key, pair => pair.Value with { Placement = pair.Value.Placement with { Features = [.. pair.Value.Placement.Features] } });
    }

    /// <inheritdoc/>
    public SourcePlacementResolutionResult Resolve(SourcePlacementResolutionRequest request)
    {
        var structures = _snapshot.Structures.Where(structure => structure.Subject == request.SourceOwner.Subject).ToArray();
        var failures = _snapshot.Diagnostics.Where(diagnostic => diagnostic.Subject is null || diagnostic.Subject == request.SourceOwner.Subject).ToArray();
        if (failures.Length > 0) return new() { Artifact = request.Artifact, Diagnostics = failures };
        if (structures.Length != 1) return Failure(request, structures.Length == 0 ? DotNetSourceStructureDiagnosticCodes.MissingSourceMapping : DotNetSourceStructureDiagnosticCodes.DuplicateSourceSubject, "Selected source owner requires exactly one fixed source structure");
        var structure = structures[0];
        if (!_policies.TryGetValue(structure.Project, out var policy)) return Failure(request, DotNetSourceStructureDiagnosticCodes.MissingSourceContext, "Selected source owner has no fixed project policy");
        var resolution = DotNetSourcePlacementDerivation.Derive(
        [
            new DotNetSourcePlacementRequest
            {
                Artifact = request.Artifact, Structure = structure, SliceKind = request.SliceKind, Policy = policy,
                SourceOwner = request.SourceOwner.Subject == request.Artifact.Subject ? null : request.SourceOwner.Subject,
                CompatibilityPolicy = _compatibility.GetValueOrDefault(request.Artifact)
            }
        ]);
        return new() { Artifact = request.Artifact, Placement = resolution.Placements.SingleOrDefault()?.Placement, Diagnostics = [.. resolution.Diagnostics] };
    }

    static SourcePlacementResolutionResult Failure(SourcePlacementResolutionRequest request, string code, string message) => new()
    {
        Artifact = request.Artifact,
        Diagnostics = [new() { Code = code, Severity = GenerationDiagnosticSeverity.Error, Outcome = GenerationDiagnosticOutcome.Unsupported, Subject = request.SourceOwner.Subject, Message = message }]
    };
}
