// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructureResolver.when_resolving;

public class and_folder_and_namespace_disagree : Specification
{
    DotNetSourceStructureResolution _result = null!;

    void Because() => _result = DotNetSourceStructureResolver.Resolve(
        new DotNetSourceStructure
        {
            Subject = new() { Value = "dotnet://Banking/Banking/Accounts.Projects.Register.Register" },
            Project = "Banking",
            ProjectRole = DotNetProjectRole.Application,
            Namespace = "Banking.Accounts.Projects.Rename",
            ProjectRelativePaths = ["Source/Accounts/Projects/Register/Register.cs"]
        },
        GenerationSliceKind.StateChange,
        new DotNetSourceStructurePolicy
        {
            FeatureRoot = "Source",
            NamespaceSegmentsToSkip = 1
        });

    [Fact] void should_fail_closed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_choose_either_placement() => _result.Placement.ShouldBeNull();
    [Fact] void should_report_the_disagreement() => _result.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.ConflictingStructure);
    [Fact] void should_retain_the_affected_subject() => _result.Diagnostics.Single().Subject.ShouldEqual(_result.Structure.Subject);
}
