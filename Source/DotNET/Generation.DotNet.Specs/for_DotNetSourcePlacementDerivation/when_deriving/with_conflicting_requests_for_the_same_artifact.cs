// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_conflicting_requests_for_the_same_artifact : Specification
{
    DotNetSourcePlacementSnapshot _snapshot = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Banking/Banking.Accounts.Register.Register" };
        var artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Command };
        var structure = new DotNetSourceStructure
        {
            Subject = subject,
            Project = "Banking",
            ProjectRole = DotNetProjectRole.Application,
            Namespace = "Banking.Accounts.Register",
            ProjectRelativePaths = ["Source/Accounts/Register/Register.cs"]
        };
        _snapshot = DotNetSourcePlacementDerivation.Derive(
        [
            Request(artifact, structure, null),
            Request(artifact, structure, "Banking")
        ]);
    }

    [Fact] void should_fail_closed() => _snapshot.IsSuccess.ShouldBeFalse();
    [Fact] void should_contribute_no_placement() => _snapshot.Placements.ShouldBeEmpty();
    [Fact] void should_report_the_conflicting_requests() => _snapshot.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.ConflictingPlacementRequests);

    static DotNetSourcePlacementRequest Request(
        ArtifactKey artifact,
        DotNetSourceStructure structure,
        string? module) => new()
        {
            Artifact = artifact,
            Structure = structure,
            SliceKind = GenerationSliceKind.StateChange,
            Policy = new DotNetSourceStructurePolicy
            {
                FeatureRoot = "Source",
                NamespaceSegmentsToSkip = 1,
                Module = module
            }
        };
}
