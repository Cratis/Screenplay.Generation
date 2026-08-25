// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class from_relocated_flat_source_checkouts : given.a_compilation
{
    string _first = null!;
    string _second = null!;

    void Because()
    {
        _first = SnapshotAt("/first-checkout");
        _second = SnapshotAt("/second-checkout");
    }

    [Fact] void should_produce_the_same_compatibility_placement_and_provenance() => _first.ShouldEqual(_second);
    [Fact] void should_not_expose_either_physical_checkout() => _first.Contains("checkout", StringComparison.Ordinal).ShouldBeFalse();

    string SnapshotAt(string root)
    {
        var compilation = CompilationFrom(new SourceFile(
            $"{root}/Ordering/PlaceOrder.cs",
            "namespace Ordering; public record PlaceOrder;"));
        var tree = compilation.SyntaxTrees.Single();
        var sourceContext = DotNetSourcePaths.Create(
            "apps/Ordering",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = tree,
                    ProjectRelativePath = "PlaceOrder.cs",
                    WorkspaceRelativePath = "apps/Ordering/PlaceOrder.cs"
                }
            ]);
        var project = new DotNetProjectCompilation
        {
            Name = "Ordering",
            Compilation = compilation,
            SourceContext = sourceContext,
            AuthoredSyntaxTrees = new HashSet<SyntaxTree> { tree }
        };
        var structure = DotNetSourceStructures.Create(new DotNetAnalysisContext([project])).Structures.Single();
        var result = DotNetSourcePlacementDerivation.Derive(
        [
            new DotNetSourcePlacementRequest
            {
                Artifact = new ArtifactKey { Subject = structure.Subject, Kind = ArtifactKind.Command },
                Structure = structure,
                SliceKind = GenerationSliceKind.StateChange,
                Policy = new(),
                CompatibilityPolicy = new DotNetSourcePlacementCompatibilityPolicy
                {
                    Placement = new ArtifactPlacement
                    {
                        Module = "Commerce",
                        Features = ["Orders"],
                        Slice = "Place",
                        SliceKind = GenerationSliceKind.StateChange
                    }
                }
            }
        ]);
        var placement = result.Placements.Single();
        return $"{placement.Artifact.Subject.Value}|{placement.Structure.Project}|{placement.Structure.Source?.Path}|" +
               $"{placement.Structure.Source?.FileIdentity}|{placement.Policy.Version}|{placement.CompatibilityPolicy?.Version}|" +
               $"{placement.UsedCompatibilityPlacement}|{placement.CompatibilityReasonCode}|{placement.Placement.Module}|{placement.Placement.Slice}";
    }
}
