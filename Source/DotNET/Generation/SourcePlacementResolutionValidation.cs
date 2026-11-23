// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class SourcePlacementResolutionValidation
{
    public static SourcePlacementResolutionResult Validate(SourcePlacementResolutionResult? result, ArtifactKey artifact, GenerationSliceKind kind, GenerationFact[] inputs)
    {
        if (HasIdentity(result, artifact, kind) && ValidPlacement(result!.Placement!) && ValidDiagnostics(result.Diagnostics))
        {
            return result with { Placement = result.Placement! with { Features = [.. result.Placement!.Features] } };
        }

        return new()
        {
            Artifact = artifact,
            Diagnostics = [.. result?.Diagnostics?.Where(diagnostic => diagnostic is not null) ?? [], SourcePlacementCandidateDerivation.Diagnostic(GenerationDiagnosticCodes.InvalidSourceOwnerCandidate, GenerationDiagnosticOutcome.Unsupported, inputs, "The source placement resolver did not return a valid placement for the exact requested artifact and semantic kind")]
        };
    }

    static bool HasIdentity(SourcePlacementResolutionResult? result, ArtifactKey artifact, GenerationSliceKind kind) =>
        result?.Artifact == artifact && result?.Placement?.SliceKind == kind;

    static bool ValidPlacement(ArtifactPlacement placement) => IsName(placement.Module) && IsName(placement.Slice) && placement.Features?.All(IsName) == true;

    static bool ValidDiagnostics(IReadOnlyList<GenerationDiagnostic>? diagnostics) => diagnostics?.All(diagnostic => diagnostic is not null && diagnostic.Severity != GenerationDiagnosticSeverity.Error) == true;

    static bool IsName(string name) => !string.IsNullOrWhiteSpace(name) && name == name.Trim() && !name.Any(char.IsControl);
}
