// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_explicit_compatibility_for_flat_sources : Specification
{
    DotNetSourcePlacementSnapshot _rootFile = null!;
    DotNetSourcePlacementSnapshot _oneSegmentNamespace = null!;
    DotNetSourcePlacementSnapshot _configured = null!;

    void Because()
    {
        _rootFile = Derive("PlaceOrder.cs", string.Empty, new());
        _oneSegmentNamespace = Derive("PlaceOrder.cs", "Ordering", new());
        _configured = Derive(
            "PlaceOrder.cs",
            "Application",
            new DotNetSourceStructurePolicy
            {
                FeatureRoot = null,
                NamespaceSegmentsToSkip = 1,
                Module = "Commerce"
            });
    }

    [Fact] void should_accept_the_explicit_compatibility_placement_for_a_root_file() => _rootFile.IsSuccess.ShouldBeTrue();
    [Fact] void should_accept_the_explicit_compatibility_placement_for_a_one_segment_namespace() => _oneSegmentNamespace.IsSuccess.ShouldBeTrue();
    [Fact] void should_accept_compatibility_after_configured_module_and_namespace_skip_still_leave_insufficient_structure() => _configured.IsSuccess.ShouldBeTrue();
    [Fact] void should_expose_that_compatibility_was_used() => Results.All(_ => _.UsedCompatibilityPlacement).ShouldBeTrue();
    [Fact] void should_expose_the_only_diagnostic_that_authorized_compatibility() => Results.All(_ => _.CompatibilityReasonCode == DotNetSourceStructureDiagnosticCodes.InsufficientStructure).ShouldBeTrue();
    [Fact] void should_use_the_exact_explicit_placement() => Results.All(_ => Canonical(_.Placement) == "Ordering/Orders/Place:StateChange").ShouldBeTrue();
    [Fact] void should_retain_the_strict_policy_for_provenance() => Canonical(_configured.Placements.Single().Policy).ShouldEqual("1:<absent>:1:Commerce");
    [Fact] void should_retain_the_explicit_compatibility_policy_for_provenance() => Results.All(_ => _.CompatibilityPolicy is { Version: 1 } policy && Canonical(policy.Placement) == "Ordering/Orders/Place:StateChange").ShouldBeTrue();
    [Fact] void should_return_no_blocking_diagnostics() => new[] { _rootFile, _oneSegmentNamespace, _configured }.All(_ => _.Diagnostics.Count == 0).ShouldBeTrue();

    IReadOnlyList<DotNetSourcePlacement> Results =>
    [
        _rootFile.Placements.Single(),
        _oneSegmentNamespace.Placements.Single(),
        _configured.Placements.Single()
    ];

    static DotNetSourcePlacementSnapshot Derive(
        string path,
        string declaredNamespace,
        DotNetSourceStructurePolicy policy)
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
                    Namespace = declaredNamespace,
                    ProjectRelativePaths = [path]
                },
                SliceKind = GenerationSliceKind.StateChange,
                Policy = policy,
                CompatibilityPolicy = CompatibilityPolicy()
            }
        ]);
    }

    static string Canonical(ArtifactPlacement placement) =>
        $"{placement.Module}/{string.Join('/', placement.Features)}/{placement.Slice}:{placement.SliceKind}";

    static string Canonical(DotNetSourceStructurePolicy policy) =>
        $"{policy.Version}:{policy.FeatureRoot ?? "<absent>"}:{policy.NamespaceSegmentsToSkip}:{policy.Module ?? "<absent>"}";

    static DotNetSourcePlacementCompatibilityPolicy CompatibilityPolicy() => new()
    {
        Placement = CompatibilityPlacement()
    };

    static ArtifactPlacement CompatibilityPlacement() => new()
    {
        Module = "Ordering",
        Features = ["Orders"],
        Slice = "Place",
        SliceKind = GenerationSliceKind.StateChange
    };
}
