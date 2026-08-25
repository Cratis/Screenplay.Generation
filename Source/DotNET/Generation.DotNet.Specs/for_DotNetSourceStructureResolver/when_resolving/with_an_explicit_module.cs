// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructureResolver.when_resolving;

public class with_an_explicit_module : Specification
{
    DotNetSourceStructureResolution _result = null!;

    void Because() => _result = DotNetSourceStructureResolver.Resolve(
        new DotNetSourceStructure
        {
            Subject = new() { Value = "dotnet://Banking/Banking/Projects.Register.Register" },
            Project = "Banking",
            ProjectRole = DotNetProjectRole.Application,
            Namespace = "Banking.Projects.Register",
            ProjectRelativePaths = ["Source/Projects/Register/Register.cs"]
        },
        GenerationSliceKind.StateChange,
        new DotNetSourceStructurePolicy
        {
            FeatureRoot = "Source",
            NamespaceSegmentsToSkip = 1,
            Module = "Application"
        });

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_collapse_the_source_root_into_the_configured_module() => _result.Placement!.Module.ShouldEqual("Application");
    [Fact] void should_keep_the_source_root_as_a_feature() => _result.Placement!.Features.ShouldContainOnly("Projects");
    [Fact] void should_keep_the_final_segment_as_the_slice() => _result.Placement!.Slice.ShouldEqual("Register");
}
