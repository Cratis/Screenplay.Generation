// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_mutated_compatibility_policy_input : Specification
{
    DotNetSourcePlacement _placement = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Ordering/Ordering/PlaceOrder" };
        var features = new List<string> { "Orders" };
        var compatibility = new DotNetSourcePlacementCompatibilityPolicy
        {
            Placement = new ArtifactPlacement
            {
                Module = "Commerce",
                Features = features,
                Slice = "Place",
                SliceKind = GenerationSliceKind.StateChange
            }
        };
        _placement = DotNetSourcePlacementDerivation.Derive(
        [
            new DotNetSourcePlacementRequest
            {
                Artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Command },
                Structure = new DotNetSourceStructure
                {
                    Subject = subject,
                    Project = "Ordering/Ordering",
                    ProjectRole = DotNetProjectRole.Application,
                    Namespace = "Ordering",
                    ProjectRelativePaths = ["PlaceOrder.cs"]
                },
                SliceKind = GenerationSliceKind.StateChange,
                Policy = new(),
                CompatibilityPolicy = compatibility
            }
        ]).Placements.Single();

        features[0] = "Changed";
    }

    [Fact] void should_retain_an_immutable_compatibility_placement_snapshot() => _placement.Placement.Features.ShouldContainOnly("Orders");
    [Fact] void should_retain_immutable_compatibility_policy_provenance() => _placement.CompatibilityPolicy!.Placement.Features.ShouldContainOnly("Orders");
}
