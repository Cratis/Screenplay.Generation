// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_duplicate_identical_requests : Specification
{
    DotNetSourcePlacementSnapshot _snapshot = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Banking/Banking.Accounts.Register.Register" };
        var request = new DotNetSourcePlacementRequest
        {
            Artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Command },
            Structure = new DotNetSourceStructure
            {
                Subject = subject,
                Project = "Banking",
                ProjectRole = DotNetProjectRole.Application,
                Namespace = "Banking.Accounts.Register",
                ProjectRelativePaths = ["Source/Accounts/Register/Register.cs"]
            },
            SliceKind = GenerationSliceKind.StateChange,
            Policy = new DotNetSourceStructurePolicy
            {
                FeatureRoot = "Source",
                NamespaceSegmentsToSkip = 1
            }
        };

        _snapshot = DotNetSourcePlacementDerivation.Derive([request, request]);
    }

    [Fact] void should_succeed() => _snapshot.IsSuccess.ShouldBeTrue();
    [Fact] void should_execute_the_identical_request_once() => _snapshot.Placements.Count.ShouldEqual(1);
}
