// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructureResolver.when_resolving;

public class with_application_and_specification_projects : Specification
{
    DotNetSourceStructureResolution _application = null!;
    DotNetSourceStructureResolution _specifications = null!;

    void Because()
    {
        var policy = new DotNetSourceStructurePolicy
        {
            FeatureRoot = "Source",
            NamespaceSegmentsToSkip = 1
        };
        _application = Resolve("Banking", DotNetProjectRole.Application, policy);
        _specifications = Resolve("Banking.Specs", DotNetProjectRole.Specifications, policy);
    }

    [Fact] void should_resolve_the_same_target_structure() => _application.Placement.ShouldEqual(_specifications.Placement);
    [Fact] void should_retain_the_application_project_role() => _application.Structure.ProjectRole.ShouldEqual(DotNetProjectRole.Application);
    [Fact] void should_retain_the_specification_project_role() => _specifications.Structure.ProjectRole.ShouldEqual(DotNetProjectRole.Specifications);
    [Fact] void should_keep_the_project_identities_distinct() => _application.Structure.Project.ShouldNotEqual(_specifications.Structure.Project);

    static DotNetSourceStructureResolution Resolve(
        string project,
        DotNetProjectRole role,
        DotNetSourceStructurePolicy policy) => DotNetSourceStructureResolver.Resolve(
            new DotNetSourceStructure
            {
                Subject = new() { Value = $"dotnet://{project}/Banking/Accounts.Register.Register" },
                Project = project,
                ProjectRole = role,
                Namespace = "Banking.Accounts.Register",
                ProjectRelativePaths = ["Source/Accounts/Register/Register.cs"]
            },
            GenerationSliceKind.StateChange,
            policy);
}
