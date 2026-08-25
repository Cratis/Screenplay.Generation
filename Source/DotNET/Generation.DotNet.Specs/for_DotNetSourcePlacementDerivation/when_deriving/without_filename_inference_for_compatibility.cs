// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class without_filename_inference_for_compatibility : Specification
{
    IReadOnlyList<DotNetSourcePlacementSnapshot> _snapshots = null!;

    void Because()
    {
        _snapshots =
        [
            Derive("Place.cs"),
            Derive("Place.Order.cs"),
            Derive("Place.g.cs"),
            Derive("PlaceOrder"),
            Derive(".cs"),
            Derive("CON.cs"),
            Derive("注文.cs")
        ];
    }

    [Fact] void should_accept_every_safe_filename() => _snapshots.All(_ => _.IsSuccess).ShouldBeTrue();
    [Fact] void should_use_explicit_compatibility_for_every_safe_filename() => _snapshots.Select(_ => _.Placements.Single()).All(_ => _.UsedCompatibilityPlacement).ShouldBeTrue();
    [Fact] void should_not_derive_module_feature_or_slice_from_the_filename() => _snapshots.Select(_ => _.Placements.Single().Placement).All(_ => Canonical(_) == "Commerce/Orders/Submit").ShouldBeTrue();

    static DotNetSourcePlacementSnapshot Derive(string filename)
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
                    Namespace = "Ordering",
                    ProjectRelativePaths = [filename]
                },
                SliceKind = GenerationSliceKind.StateChange,
                Policy = new(),
                CompatibilityPolicy = new DotNetSourcePlacementCompatibilityPolicy
                {
                    Placement = new ArtifactPlacement
                    {
                        Module = "Commerce",
                        Features = ["Orders"],
                        Slice = "Submit",
                        SliceKind = GenerationSliceKind.StateChange
                    }
                }
            }
        ]);
    }

    static string Canonical(ArtifactPlacement placement) =>
        string.Join('/', new[] { placement.Module }.Concat(placement.Features).Append(placement.Slice));
}
