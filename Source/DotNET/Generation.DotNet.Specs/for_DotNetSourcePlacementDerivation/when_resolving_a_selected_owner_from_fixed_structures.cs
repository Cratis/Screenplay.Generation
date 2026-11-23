// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation;

public class when_resolving_a_selected_owner_from_fixed_structures : Specification
{
    SourcePlacementResolutionResult _result = null!;
    readonly ArtifactKey _artifact = new() { Subject = new SubjectId { Value = "dotnet://Application/CustomerRegistered" }, Kind = ArtifactKind.Event };

    void Because()
    {
        var owner = new ArtifactKey { Subject = new SubjectId { Value = "dotnet://Application/RegisterCustomer" }, Kind = ArtifactKind.Command };
        var paths = new List<string> { "Customers/Registration/Register/RegisterCustomer.cs" };
        var resolver = new DotNetArtifactSourcePlacementResolver(
            new DotNetSourceStructureSnapshot
            {
                Structures = [new DotNetSourceStructure { Subject = owner.Subject, Project = "Application", ProjectRole = DotNetProjectRole.Application, Namespace = "Customers.Registration.Register", ProjectRelativePaths = paths }]
            },
            new Dictionary<string, DotNetSourceStructurePolicy> { ["Application"] = new() });
        paths[0] = "/changed/after/snapshot.cs";
        _result = resolver.Resolve(new SourcePlacementResolutionRequest { Artifact = _artifact, SourceOwner = owner, SliceKind = GenerationSliceKind.StateChange });
    }

    [Fact] void should_map_the_command_source_for_the_event() => _result.Placement!.Slice.ShouldEqual("Register");
    [Fact] void should_preserve_the_exact_requested_artifact() => _result.Artifact.ShouldEqual(_artifact);
    [Fact] void should_not_read_mutated_discovery_state() => _result.Diagnostics.ShouldBeEmpty();
}
