// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_malformed_or_unknown_compatibility_policy : Specification
{
    IReadOnlyList<DotNetSourcePlacementSnapshot> _snapshots = null!;

    void Because()
    {
        _snapshots =
        [
            Derive(new DotNetSourcePlacementCompatibilityPolicy { Version = 2, Placement = ValidPlacement() }),
            Derive(new DotNetSourcePlacementCompatibilityPolicy { Version = 2, Placement = ValidPlacement() }, strictStructure: true),
            Derive(new DotNetSourcePlacementCompatibilityPolicy { Placement = null! }),
            Derive(Policy(ValidPlacement() with { Module = " " })),
            Derive(Policy(ValidPlacement() with { Module = "Commerce/Orders" })),
            Derive(Policy(ValidPlacement() with { Features = ["Orders", ".."] })),
            Derive(Policy(ValidPlacement() with { Features = null! })),
            Derive(Policy(ValidPlacement() with { Slice = "." })),
            Derive(Policy(ValidPlacement() with { SliceKind = GenerationSliceKind.StateView })),
            Derive(Policy(ValidPlacement() with { SliceKind = (GenerationSliceKind)int.MaxValue }))
        ];
    }

    [Fact] void should_fail_closed_for_every_malformed_or_unknown_value() => _snapshots.All(_ => !_.IsSuccess).ShouldBeTrue();
    [Fact] void should_contribute_no_placement() => _snapshots.All(_ => _.Placements.Count == 0).ShouldBeTrue();
    [Fact] void should_report_the_stable_compatibility_policy_diagnostic() => _snapshots.All(_ => _.Diagnostics.Single().Code == DotNetSourceStructureDiagnosticCodes.UnsupportedCompatibilityPolicy).ShouldBeTrue();
    [Fact] void should_report_unsupported_outcomes() => _snapshots.All(_ => _.Diagnostics.Single().Outcome == GenerationDiagnosticOutcome.Unsupported).ShouldBeTrue();

    static DotNetSourcePlacementSnapshot Derive(
        DotNetSourcePlacementCompatibilityPolicy compatibilityPolicy,
        bool strictStructure = false)
    {
        var subject = new SubjectId { Value = "dotnet://Ordering/Ordering/PlaceOrder" };
        return DotNetSourcePlacementDerivation.Derive(
        [
            new DotNetSourcePlacementRequest
            {
                Artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Command },
                Structure = new DotNetSourceStructure
                {
                    Subject = subject,
                    Project = "Ordering/Ordering",
                    ProjectRole = DotNetProjectRole.Application,
                    Namespace = strictStructure ? "Application.Orders.Place" : "Ordering",
                    ProjectRelativePaths = strictStructure ? ["Source/Orders/Place/PlaceOrder.cs"] : ["PlaceOrder.cs"]
                },
                SliceKind = GenerationSliceKind.StateChange,
                Policy = strictStructure
                    ? new DotNetSourceStructurePolicy { FeatureRoot = "Source", NamespaceSegmentsToSkip = 1 }
                    : new(),
                CompatibilityPolicy = compatibilityPolicy
            }
        ]);
    }

    static DotNetSourcePlacementCompatibilityPolicy Policy(ArtifactPlacement placement) => new()
    {
        Placement = placement
    };

    static ArtifactPlacement ValidPlacement() => new()
    {
        Module = "Commerce",
        Features = ["Orders"],
        Slice = "Place",
        SliceKind = GenerationSliceKind.StateChange
    };
}
