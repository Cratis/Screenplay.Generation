// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_exact_alternate_source_owners : Specification
{
    DotNetSourcePlacementSnapshot _method = null!;
    DotNetSourcePlacementSnapshot _query = null!;
    DotNetSourcePlacementSnapshot _reducer = null!;
    DotNetSourcePlacementSnapshot _mismatch = null!;
    DotNetSourcePlacementSnapshot _reversed = null!;
    DotNetSourcePlacementSnapshot _redundant = null!;
    DotNetSourcePlacementSnapshot _malformed = null!;

    void Because()
    {
        _method = Derive(ArtifactKind.Command, "#method/M:Handlers.Place(PlaceOrder)", "Handlers", GenerationSliceKind.StateChange);
        _query = Derive(ArtifactKind.Query, "#method/M:Endpoints.Get(System.Guid)", "Endpoints", GenerationSliceKind.StateView);
        _reducer = Derive(ArtifactKind.Reducer, "#reducer", "OrderSummary", GenerationSliceKind.StateView);

        var artifactSubject = Subject("#method/M:Endpoints.Get(System.Guid)");
        _mismatch = DotNetSourcePlacementDerivation.Derive(
        [
            Request(
                ArtifactKind.Query,
                artifactSubject,
                Subject("Endpoints"),
                Structure(Subject("OtherOwner")),
                GenerationSliceKind.StateView)
        ]);
        _reversed = DotNetSourcePlacementDerivation.Derive(
        [
            Request(
                ArtifactKind.Query,
                artifactSubject,
                Subject("Endpoints"),
                Structure(artifactSubject),
                GenerationSliceKind.StateView)
        ]);
        _redundant = DotNetSourcePlacementDerivation.Derive(
        [
            Request(
                ArtifactKind.Query,
                artifactSubject,
                artifactSubject,
                Structure(artifactSubject),
                GenerationSliceKind.StateView)
        ]);
        _malformed = DotNetSourcePlacementDerivation.Derive(
        [
            Request(
                ArtifactKind.Query,
                artifactSubject,
                new SubjectId { Value = " " },
                Structure(new SubjectId { Value = " " }),
                GenerationSliceKind.StateView)
        ]);
    }

    [Fact] void should_place_a_method_backed_command_from_its_exact_owner() => _method.IsSuccess.ShouldBeTrue();
    [Fact] void should_place_a_query_from_its_exact_containing_type() => _query.IsSuccess.ShouldBeTrue();
    [Fact] void should_place_a_synthetic_reducer_from_its_exact_model() => _reducer.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_fixed_source_owner_without_rewriting_the_snapshot() => Successful.All(_ => _.Structure.Subject == _.SourceOwner && _.Structure.Subject != _.Artifact.Subject).ShouldBeTrue();
    [Fact] void should_prefer_strict_source_placement_when_the_exact_owner_has_sufficient_structure() => Successful.All(_ => !_.UsedCompatibilityPlacement && _.Placement.Module == "Orders" && _.Placement.Slice == "Summary").ShouldBeTrue();
    [Fact] void should_fail_closed_for_an_arbitrary_owner_mismatch() => _mismatch.IsSuccess.ShouldBeFalse();
    [Fact] void should_contribute_no_placement_for_an_arbitrary_owner_mismatch() => _mismatch.Placements.ShouldBeEmpty();
    [Fact] void should_report_the_arbitrary_owner_mismatch() => _mismatch.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.MismatchedPlacementSubject);
    [Fact] void should_reject_a_reversed_owner_reference() => Rejected(_reversed);
    [Fact] void should_reject_a_redundant_same_subject_owner_reference() => Rejected(_redundant);
    [Fact] void should_reject_a_malformed_owner_reference() => Rejected(_malformed);

    IReadOnlyList<DotNetSourcePlacement> Successful =>
    [
        _method.Placements.Single(),
        _query.Placements.Single(),
        _reducer.Placements.Single()
    ];

    static void Rejected(DotNetSourcePlacementSnapshot snapshot) =>
        (!snapshot.IsSuccess && snapshot.Placements.Count == 0 && snapshot.Diagnostics.Single().Code == DotNetSourceStructureDiagnosticCodes.MismatchedPlacementSubject).ShouldBeTrue();

    static DotNetSourcePlacementSnapshot Derive(
        ArtifactKind kind,
        string artifactSuffix,
        string ownerSuffix,
        GenerationSliceKind sliceKind)
    {
        var artifactSubject = Subject(artifactSuffix);
        var owner = Subject(ownerSuffix);
        return DotNetSourcePlacementDerivation.Derive(
        [
            Request(kind, artifactSubject, owner, Structure(owner), sliceKind)
        ]);
    }

    static DotNetSourcePlacementRequest Request(
        ArtifactKind kind,
        SubjectId artifactSubject,
        SubjectId sourceOwner,
        DotNetSourceStructure structure,
        GenerationSliceKind sliceKind) => new()
        {
            Artifact = new ArtifactKey { Subject = artifactSubject, Kind = kind },
            Structure = structure,
            SourceOwner = sourceOwner,
            SliceKind = sliceKind,
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
                    SliceKind = sliceKind
                }
            }
        };

    static DotNetSourceStructure Structure(SubjectId owner) => new()
    {
        Subject = owner,
        Project = "Ordering/Ordering",
        ProjectRole = DotNetProjectRole.Application,
        Namespace = "Application.Orders.Summary",
        ProjectRelativePaths = ["Source/Orders/Summary/Summary.cs"]
    };

    static SubjectId Subject(string suffix) => new()
    {
        Value = $"dotnet://Ordering/Ordering/{suffix}"
    };
}
