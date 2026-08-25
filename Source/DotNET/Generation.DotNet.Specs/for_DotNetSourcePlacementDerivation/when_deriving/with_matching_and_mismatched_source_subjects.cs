// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_matching_and_mismatched_source_subjects : Specification
{
    DotNetSourcePlacementSnapshot _forward = null!;
    DotNetSourcePlacementSnapshot _reversed = null!;

    void Because()
    {
        var artifactSubject = new SubjectId { Value = "dotnet://Banking/Banking/Banking.Accounts.Register.Register" };
        var matching = Request(artifactSubject, artifactSubject);
        var mismatched = Request(
            artifactSubject,
            new SubjectId { Value = "dotnet://Banking/Banking/Banking.Accounts.Rename.Rename" });

        _forward = DotNetSourcePlacementDerivation.Derive([matching, mismatched]);
        _reversed = DotNetSourcePlacementDerivation.Derive([mismatched, matching]);
    }

    [Fact] void should_fail_closed_in_both_orders() => new[] { _forward, _reversed }.All(_ => !_.IsSuccess).ShouldBeTrue();
    [Fact] void should_contribute_no_order_selected_placement() => new[] { _forward, _reversed }.All(_ => _.Placements.Count == 0).ShouldBeTrue();
    [Fact] void should_report_the_same_conflict_in_both_orders() => Canonical(_forward).ShouldEqual(Canonical(_reversed));
    [Fact] void should_report_conflicting_requests() => _forward.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.ConflictingPlacementRequests);

    static string Canonical(DotNetSourcePlacementSnapshot snapshot)
    {
        var diagnostic = snapshot.Diagnostics.Single();
        return $"{diagnostic.Code}:{diagnostic.Outcome}:{diagnostic.Subject}:{diagnostic.Message}";
    }

    static DotNetSourcePlacementRequest Request(SubjectId artifactSubject, SubjectId structureSubject) => new()
    {
        Artifact = new ArtifactKey { Subject = artifactSubject, Kind = ArtifactKind.Command },
        Structure = new DotNetSourceStructure
        {
            Subject = structureSubject,
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
    };
}
