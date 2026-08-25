// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructureResolver.when_resolving;

public class with_partial_declarations_in_conflicting_folders : Specification
{
    DotNetSourceStructureResolution _result = null!;

    void Because() => _result = DotNetSourceStructureResolver.Resolve(
        new DotNetSourceStructure
        {
            Subject = new() { Value = "dotnet://Banking/Banking/Accounts.Register.Register" },
            Project = "Banking",
            ProjectRole = DotNetProjectRole.Application,
            Namespace = "Banking.Accounts.Register",
            ProjectRelativePaths =
            [
                "Source/Accounts/Register/Register.cs",
                "Source/Accounts/Rename/Register.Partial.cs"
            ]
        },
        GenerationSliceKind.StateChange,
        new DotNetSourceStructurePolicy
        {
            FeatureRoot = "Source",
            NamespaceSegmentsToSkip = 1
        });

    [Fact] void should_fail_closed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_choose_one_authored_declaration() => _result.Placement.ShouldBeNull();
    [Fact] void should_report_the_conflicting_source_structure() => _result.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.ConflictingStructure);
}
