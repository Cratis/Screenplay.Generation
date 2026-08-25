// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_strict_defaults_and_compatible_discriminators : Specification
{
    DotNetSourcePlacementSnapshot _strictSuccess = null!;
    DotNetSourcePlacementSnapshot _strictInsufficient = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Ordering/Ordering/PlaceOrder" };
        _strictSuccess = DotNetSourcePlacementDerivation.Derive(
        [
            Request(subject, "Source/Orders/Place/PlaceOrder.cs", "Application.Orders.Place")
        ]);
        _strictInsufficient = DotNetSourcePlacementDerivation.Derive(
        [
            Request(subject, "PlaceOrder.cs", "Ordering")
        ]);
    }

    [Fact] void should_keep_existing_request_initializers_source_compatible() => _strictSuccess.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_use_compatibility_by_default() => _strictSuccess.Placements.Single().UsedCompatibilityPlacement.ShouldBeFalse();
    [Fact] void should_expose_no_compatibility_policy_or_reason_by_default() => (_strictSuccess.Placements.Single().CompatibilityPolicy is null && _strictSuccess.Placements.Single().CompatibilityReasonCode is null).ShouldBeTrue();
    [Fact] void should_remain_fail_closed_for_insufficient_flat_structure_by_default() => _strictInsufficient.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.InsufficientStructure);
    [Fact] void should_preserve_project_role_discriminators() => ((int)DotNetProjectRole.Unknown, (int)DotNetProjectRole.Application, (int)DotNetProjectRole.Specifications).ShouldEqual((-1, 0, 1));
    [Fact] void should_preserve_slice_kind_discriminators() => ((int)GenerationSliceKind.Unknown, (int)GenerationSliceKind.StateChange, (int)GenerationSliceKind.StateView, (int)GenerationSliceKind.Automation, (int)GenerationSliceKind.Translate).ShouldEqual((-1, 0, 1, 2, 3));
    [Fact] void should_preserve_method_query_and_reducer_artifact_discriminators() => ((int)ArtifactKind.Command, (int)ArtifactKind.Query, (int)ArtifactKind.Reducer).ShouldEqual((3, 8, 10));

    static DotNetSourcePlacementRequest Request(SubjectId subject, string path, string declaredNamespace) => new()
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
        Policy = new DotNetSourceStructurePolicy
        {
            FeatureRoot = path.StartsWith("Source/", StringComparison.Ordinal) ? "Source" : null,
            NamespaceSegmentsToSkip = path.StartsWith("Source/", StringComparison.Ordinal) ? 1 : 0
        }
    };
}
