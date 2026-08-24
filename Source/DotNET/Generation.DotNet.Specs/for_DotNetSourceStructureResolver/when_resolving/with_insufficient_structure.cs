// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructureResolver.when_resolving;

public class with_insufficient_structure : Specification
{
    DotNetSourceStructureResolution _result = null!;

    void Because() => _result = DotNetSourceStructureResolver.Resolve(
        new DotNetSourceStructure
        {
            Subject = new() { Value = "dotnet://Banking/Banking/Register" },
            Project = "Banking",
            ProjectRole = DotNetProjectRole.Application,
            Namespace = "Banking",
            ProjectRelativePaths = ["Register.cs"]
        },
        GenerationSliceKind.StateChange,
        new DotNetSourceStructurePolicy { NamespaceSegmentsToSkip = 1 });

    [Fact] void should_fail_closed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_invent_a_module_or_slice() => _result.Placement.ShouldBeNull();
    [Fact] void should_report_insufficient_structure() => _result.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.InsufficientStructure);
}
