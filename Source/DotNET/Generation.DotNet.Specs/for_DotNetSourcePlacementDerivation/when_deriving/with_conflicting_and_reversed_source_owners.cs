// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_conflicting_and_reversed_source_owners : Specification
{
    DotNetSourcePlacementSnapshot _forward = null!;
    DotNetSourcePlacementSnapshot _reversed = null!;
    DotNetSourcePlacementSnapshot _duplicateForward = null!;
    DotNetSourcePlacementSnapshot _duplicateReversed = null!;

    void Because()
    {
        var artifactSubject = Subject("#reducer");
        var first = Request(artifactSubject, Subject("OrderSummary"));
        var second = Request(artifactSubject, Subject("OrderProjection"));
        _forward = DotNetSourcePlacementDerivation.Derive([first, second]);
        _reversed = DotNetSourcePlacementDerivation.Derive([second, first]);
        _duplicateForward = DotNetSourcePlacementDerivation.Derive([first, first]);
        _duplicateReversed = DotNetSourcePlacementDerivation.Derive(new[] { first, first }.AsEnumerable().Reverse());
    }

    [Fact] void should_fail_closed_for_conflicting_exact_owners() => new[] { _forward, _reversed }.All(_ => !_.IsSuccess).ShouldBeTrue();
    [Fact] void should_contribute_no_order_selected_placement() => new[] { _forward, _reversed }.All(_ => _.Placements.Count == 0).ShouldBeTrue();
    [Fact] void should_report_conflicting_requests_for_both_orders() => new[] { _forward, _reversed }.All(_ => _.Diagnostics.Single().Code == DotNetSourceStructureDiagnosticCodes.ConflictingPlacementRequests).ShouldBeTrue();
    [Fact] void should_report_byte_equivalent_conflicts_for_both_orders() => Canonical(_forward).ShouldEqual(Canonical(_reversed));
    [Fact] void should_collapse_duplicate_exact_owner_requests_in_both_orders() => new[] { _duplicateForward, _duplicateReversed }.All(_ => _.IsSuccess && _.Placements.Count == 1).ShouldBeTrue();
    [Fact] void should_produce_the_same_exact_owner_result_for_duplicate_reversals() => Canonical(_duplicateForward).ShouldEqual(Canonical(_duplicateReversed));

    static string Canonical(DotNetSourcePlacementSnapshot snapshot) => string.Join(
        '|',
        snapshot.Placements.Select(_ => $"{_.Artifact.Subject.Value}:{_.SourceOwner?.Value}:{_.Structure.Subject.Value}:{_.Placement.Module}:{_.Placement.Slice}")
            .Concat(snapshot.Diagnostics.Select(_ => $"{_.Code}:{_.Outcome}:{_.Subject?.Value}:{_.Message}")));

    static DotNetSourcePlacementRequest Request(SubjectId artifactSubject, SubjectId sourceOwner) => new()
    {
        Artifact = new ArtifactKey { Subject = artifactSubject, Kind = ArtifactKind.Reducer },
        Structure = new DotNetSourceStructure
        {
            Subject = sourceOwner,
            Project = "Ordering/Ordering",
            ProjectRole = DotNetProjectRole.Application,
            Namespace = "Application.Orders.Summary",
            ProjectRelativePaths = ["Source/Orders/Summary/Summary.cs"]
        },
        SourceOwner = sourceOwner,
        SliceKind = GenerationSliceKind.StateView,
        Policy = new DotNetSourceStructurePolicy
        {
            FeatureRoot = "Source",
            NamespaceSegmentsToSkip = 1
        },
        CompatibilityPolicy = new DotNetSourcePlacementCompatibilityPolicy
        {
            Placement = new ArtifactPlacement
            {
                Module = "Compatibility",
                Slice = "Fallback",
                SliceKind = GenerationSliceKind.StateView
            }
        }
    };

    static SubjectId Subject(string suffix) => new()
    {
        Value = $"dotnet://Ordering/Ordering/{suffix}"
    };
}
