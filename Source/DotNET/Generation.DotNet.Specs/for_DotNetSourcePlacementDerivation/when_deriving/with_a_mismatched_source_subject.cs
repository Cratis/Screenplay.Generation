// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_a_mismatched_source_subject : Specification
{
    DotNetSourcePlacementSnapshot _snapshot = null!;

    void Because() => _snapshot = DotNetSourcePlacementDerivation.Derive(
    [
        new DotNetSourcePlacementRequest
        {
            Artifact = new ArtifactKey
            {
                Subject = new SubjectId { Value = "dotnet://Banking/Banking/Banking.Accounts.Register.Register" },
                Kind = ArtifactKind.Command
            },
            Structure = new DotNetSourceStructure
            {
                Subject = new SubjectId { Value = "dotnet://Banking/Banking/Banking.Accounts.Rename.Rename" },
                Project = "Banking",
                ProjectRole = DotNetProjectRole.Application,
                Namespace = "Banking.Accounts.Rename",
                ProjectRelativePaths = ["Source/Accounts/Rename/Rename.cs"]
            },
            SliceKind = GenerationSliceKind.StateChange,
            Policy = new DotNetSourceStructurePolicy
            {
                FeatureRoot = "Source",
                NamespaceSegmentsToSkip = 1
            }
        }
    ]);

    [Fact] void should_fail_closed() => _snapshot.IsSuccess.ShouldBeFalse();
    [Fact] void should_contribute_no_placement() => _snapshot.Placements.ShouldBeEmpty();
    [Fact] void should_report_the_identity_mismatch() => _snapshot.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.MismatchedPlacementSubject);
}
