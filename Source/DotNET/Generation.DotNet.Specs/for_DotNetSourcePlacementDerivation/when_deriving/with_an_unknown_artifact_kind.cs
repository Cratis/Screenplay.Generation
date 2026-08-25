// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_an_unknown_artifact_kind : Specification
{
    DotNetSourcePlacementSnapshot _snapshot = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Banking/Banking.Accounts.Register.Register" };
        _snapshot = DotNetSourcePlacementDerivation.Derive(
        [
            new DotNetSourcePlacementRequest
            {
                Artifact = new ArtifactKey
                {
                    Subject = subject,
                    Kind = Enum.Parse<ArtifactKind>("Unknown")
                },
                Structure = new DotNetSourceStructure
                {
                    Subject = subject,
                    Project = "Banking",
                    ProjectRole = DotNetProjectRole.Application,
                    Namespace = "Banking.Accounts.Register",
                    ProjectRelativePaths = ["Source/Accounts/Register/Register.cs"]
                },
                SliceKind = GenerationSliceKind.StateChange,
                Policy = new DotNetSourceStructurePolicy
                {
                    FeatureRoot = "Source",
                    NamespaceSegmentsToSkip = 1
                }
            }
        ]);
    }

    [Fact] void should_fail_closed() => _snapshot.IsSuccess.ShouldBeFalse();
    [Fact] void should_contribute_no_placement() => _snapshot.Placements.ShouldBeEmpty();
    [Fact] void should_report_the_artifact_kind() => _snapshot.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.UnsupportedPlacementArtifactKind);
    [Fact] void should_report_an_unknown_outcome() => _snapshot.Diagnostics.Single().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Unknown);
}
