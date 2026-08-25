// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructureResolver.when_resolving;

public class with_unknown_discriminators : Specification
{
    DotNetSourceStructureResolution _role = null!;
    DotNetSourceStructureResolution _sliceKind = null!;

    void Because()
    {
        _role = Resolve(Enum.Parse<DotNetProjectRole>("Unknown"), GenerationSliceKind.StateChange);
        _sliceKind = Resolve(DotNetProjectRole.Application, Enum.Parse<GenerationSliceKind>("Unknown"));
    }

    [Fact] void should_reject_the_project_role() => _role.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.UnsupportedProjectRole);
    [Fact] void should_reject_the_slice_kind() => _sliceKind.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.UnsupportedSliceKind);
    [Fact] void should_not_derive_either_placement() => new[] { _role, _sliceKind }.All(_ => _.Placement is null).ShouldBeTrue();

    static DotNetSourceStructureResolution Resolve(DotNetProjectRole role, GenerationSliceKind sliceKind) =>
        DotNetSourceStructureResolver.Resolve(
            new DotNetSourceStructure
            {
                Subject = new() { Value = "dotnet://Banking/Banking/Accounts.Register.Register" },
                Project = "Banking",
                ProjectRole = role,
                Namespace = "Banking.Accounts.Register",
                ProjectRelativePaths = ["Source/Accounts/Register/Register.cs"]
            },
            sliceKind,
            new DotNetSourceStructurePolicy
            {
                FeatureRoot = "Source",
                NamespaceSegmentsToSkip = 1
            });
}
