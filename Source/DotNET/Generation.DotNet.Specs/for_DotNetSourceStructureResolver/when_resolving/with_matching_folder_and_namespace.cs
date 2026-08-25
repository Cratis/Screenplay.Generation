// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructureResolver.when_resolving;

public class with_matching_folder_and_namespace : Specification
{
    DotNetSourceStructureResolution _result = null!;

    void Because() => _result = DotNetSourceStructureResolver.Resolve(
        new DotNetSourceStructure
        {
            Subject = new() { Value = "dotnet://Banking/Banking/Accounts.Projects.Register.Register" },
            Project = "Banking",
            ProjectRole = DotNetProjectRole.Application,
            Namespace = "Banking.Accounts.Projects.Register",
            ProjectRelativePaths = ["Source/Accounts/Projects/Register/Register.cs"]
        },
        GenerationSliceKind.StateChange,
        new DotNetSourceStructurePolicy
        {
            FeatureRoot = "Source",
            NamespaceSegmentsToSkip = 1
        });

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_derive_the_root_as_the_module() => _result.Placement!.Module.ShouldEqual("Accounts");
    [Fact] void should_derive_intermediate_segments_as_features() => _result.Placement!.Features.ShouldContainOnly("Projects");
    [Fact] void should_derive_the_final_segment_as_the_slice() => _result.Placement!.Slice.ShouldEqual("Register");
    [Fact] void should_preserve_the_independently_established_slice_kind() => _result.Placement!.SliceKind.ShouldEqual(GenerationSliceKind.StateChange);
}
