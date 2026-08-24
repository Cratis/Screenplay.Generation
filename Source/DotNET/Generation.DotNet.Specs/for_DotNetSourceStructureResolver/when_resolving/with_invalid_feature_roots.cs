// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructureResolver.when_resolving;

public class with_invalid_feature_roots : Specification
{
    DotNetSourceStructureResolution[] _results = null!;

    void Because()
    {
        _results =
        [
            Resolve("/Source"),
            Resolve("../Source"),
            Resolve("Missing")
        ];
    }

    [Fact] void should_fail_every_resolution() => _results.All(_ => !_.IsSuccess).ShouldBeTrue();
    [Fact] void should_reject_a_rooted_feature_root() => _results[0].Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.InvalidPath);
    [Fact] void should_reject_a_traversing_feature_root() => _results[1].Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.InvalidPath);
    [Fact] void should_reject_a_missing_feature_root() => _results[2].Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.MissingFeatureRoot);

    static DotNetSourceStructureResolution Resolve(string featureRoot) => DotNetSourceStructureResolver.Resolve(
        new DotNetSourceStructure
        {
            Subject = new() { Value = "dotnet://Banking/Banking/Accounts.Register.Register" },
            Project = "Banking",
            ProjectRole = DotNetProjectRole.Application,
            Namespace = "Banking.Accounts.Register",
            ProjectRelativePaths = ["Source/Accounts/Register/Register.cs"]
        },
        GenerationSliceKind.StateChange,
        new DotNetSourceStructurePolicy
        {
            FeatureRoot = featureRoot,
            NamespaceSegmentsToSkip = 1
        });
}
