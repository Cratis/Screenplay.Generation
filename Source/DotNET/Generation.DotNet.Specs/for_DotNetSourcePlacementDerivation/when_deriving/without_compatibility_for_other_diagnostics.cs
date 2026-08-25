// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class without_compatibility_for_other_diagnostics : Specification
{
    IReadOnlyDictionary<string, DotNetSourcePlacementSnapshot> _snapshots = null!;

    void Because()
    {
        _snapshots = new Dictionary<string, DotNetSourcePlacementSnapshot>(StringComparer.Ordinal)
        {
            [DotNetSourceStructureDiagnosticCodes.UnsupportedPolicy] = Derive(policy: new DotNetSourceStructurePolicy { Version = 2 }),
            [DotNetSourceStructureDiagnosticCodes.InvalidPath] = Derive(path: "../Orders/Place.cs"),
            [DotNetSourceStructureDiagnosticCodes.MissingFeatureRoot] = Derive(path: "Other/Orders/Place.cs", policy: new DotNetSourceStructurePolicy { FeatureRoot = "Source" }),
            [DotNetSourceStructureDiagnosticCodes.ConflictingStructure] = Derive(path: "Source/Orders/Place/Place.cs", declaredNamespace: "Application.Customers.Register", policy: StandardPolicy()),
            [DotNetSourceStructureDiagnosticCodes.UnsupportedProjectRole] = Derive(projectRole: DotNetProjectRole.Unknown),
            [DotNetSourceStructureDiagnosticCodes.UnsupportedSliceKind] = Derive(sliceKind: GenerationSliceKind.Unknown),
            [DotNetSourceStructureDiagnosticCodes.InvalidNamespace] = Derive(declaredNamespace: "Application..Orders")
        };
    }

    [Fact] void should_cover_every_other_strict_resolver_diagnostic() => _snapshots.Keys.ShouldContainOnly(
        DotNetSourceStructureDiagnosticCodes.UnsupportedPolicy,
        DotNetSourceStructureDiagnosticCodes.InvalidPath,
        DotNetSourceStructureDiagnosticCodes.MissingFeatureRoot,
        DotNetSourceStructureDiagnosticCodes.ConflictingStructure,
        DotNetSourceStructureDiagnosticCodes.UnsupportedProjectRole,
        DotNetSourceStructureDiagnosticCodes.UnsupportedSliceKind,
        DotNetSourceStructureDiagnosticCodes.InvalidNamespace);
    [Fact] void should_fail_closed_for_every_other_strict_diagnostic() => _snapshots.Values.All(_ => !_.IsSuccess).ShouldBeTrue();
    [Fact] void should_contribute_no_compatibility_placement() => _snapshots.Values.All(_ => _.Placements.Count == 0).ShouldBeTrue();
    [Fact] void should_preserve_each_strict_diagnostic() => _snapshots.All(_ => _.Value.Diagnostics.Single().Code == _.Key).ShouldBeTrue();

    static DotNetSourcePlacementSnapshot Derive(
        string path = "Source/Orders/Place/Place.cs",
        string declaredNamespace = "Application.Orders.Place",
        DotNetProjectRole projectRole = DotNetProjectRole.Application,
        GenerationSliceKind sliceKind = GenerationSliceKind.StateChange,
        DotNetSourceStructurePolicy? policy = null)
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
                    ProjectRole = projectRole,
                    Namespace = declaredNamespace,
                    ProjectRelativePaths = [path]
                },
                SliceKind = sliceKind,
                Policy = policy ?? StandardPolicy(),
                CompatibilityPolicy = CompatibilityPolicy(sliceKind)
            }
        ]);
    }

    static DotNetSourceStructurePolicy StandardPolicy() => new()
    {
        FeatureRoot = "Source",
        NamespaceSegmentsToSkip = 1
    };

    static DotNetSourcePlacementCompatibilityPolicy CompatibilityPolicy(GenerationSliceKind sliceKind) => new()
    {
        Placement = new ArtifactPlacement
        {
            Module = "Ordering",
            Features = ["Orders"],
            Slice = "Place",
            SliceKind = sliceKind
        }
    };
}
