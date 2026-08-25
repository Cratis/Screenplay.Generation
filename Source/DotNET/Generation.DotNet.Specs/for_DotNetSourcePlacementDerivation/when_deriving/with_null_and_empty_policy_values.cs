// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_null_and_empty_policy_values : Specification
{
    DotNetSourcePlacementSnapshot _forward = null!;
    DotNetSourcePlacementSnapshot _reversed = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Banking/Banking.Accounts.Register.Register" };
        var structure = new DotNetSourceStructure
        {
            Subject = subject,
            Project = "Banking",
            ProjectRole = DotNetProjectRole.Application,
            Namespace = "Banking.Accounts.Register",
            ProjectRelativePaths = []
        };
        var artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Command };
        var absent = Request(artifact, structure, null);
        var empty = Request(artifact, structure, string.Empty);

        _forward = DotNetSourcePlacementDerivation.Derive([absent, empty]);
        _reversed = DotNetSourcePlacementDerivation.Derive([empty, absent]);
    }

    [Fact] void should_fail_closed_in_both_orders() => new[] { _forward, _reversed }.All(_ => !_.IsSuccess).ShouldBeTrue();
    [Fact] void should_not_collapse_absent_and_empty_values() => _forward.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.ConflictingPlacementRequests);
    [Fact] void should_report_the_same_result_in_both_orders() => _forward.Diagnostics.ShouldEqual(_reversed.Diagnostics);

    static DotNetSourcePlacementRequest Request(
        ArtifactKey artifact,
        DotNetSourceStructure structure,
        string? featureRoot) => new()
        {
            Artifact = artifact,
            Structure = structure,
            SliceKind = GenerationSliceKind.StateChange,
            Policy = new DotNetSourceStructurePolicy
            {
                FeatureRoot = featureRoot,
                NamespaceSegmentsToSkip = 1
            }
        };
}
