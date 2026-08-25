// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructureResolver.when_resolving;

public class with_an_invalid_namespace : Specification
{
    DotNetSourceStructureResolution _result = null!;

    void Because() => _result = DotNetSourceStructureResolver.Resolve(
        new DotNetSourceStructure
        {
            Subject = new() { Value = "dotnet://Banking/Banking/Accounts.Register.Register" },
            Project = "Banking",
            ProjectRole = DotNetProjectRole.Application,
            Namespace = "Banking..Accounts.Register",
            ProjectRelativePaths = ["Source/Accounts/Register/Register.cs"]
        },
        GenerationSliceKind.StateChange,
        new DotNetSourceStructurePolicy
        {
            FeatureRoot = "Source",
            NamespaceSegmentsToSkip = 1
        });

    [Fact] void should_fail_closed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_fall_back_to_the_folder() => _result.Placement.ShouldBeNull();
    [Fact] void should_report_the_invalid_namespace() => _result.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.InvalidNamespace);
}
