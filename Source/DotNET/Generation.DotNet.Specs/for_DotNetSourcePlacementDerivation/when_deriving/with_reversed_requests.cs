// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_reversed_requests : Specification
{
    DotNetSourcePlacementSnapshot _forward = null!;
    DotNetSourcePlacementSnapshot _reversed = null!;

    void Because()
    {
        DotNetSourcePlacementRequest[] requests =
        [
            Request("Banking.Accounts.Register.Register", ArtifactKind.Command, GenerationSliceKind.StateChange, "Accounts/Register"),
            Request("Banking.Accounts.Overview.Overview", ArtifactKind.ReadModel, GenerationSliceKind.StateView, "Accounts/Overview")
        ];
        _forward = DotNetSourcePlacementDerivation.Derive(requests);
        _reversed = DotNetSourcePlacementDerivation.Derive(requests.AsEnumerable().Reverse());
    }

    [Fact] void should_succeed_in_both_orders() => new[] { _forward, _reversed }.All(_ => _.IsSuccess).ShouldBeTrue();
    [Fact] void should_produce_the_same_canonical_placements() => Canonical(_forward).ShouldEqual(Canonical(_reversed));
    [Fact] void should_order_placements_by_exact_artifact_role() => string.Join(',', _forward.Placements.Select(_ => _.Artifact.Kind)).ShouldEqual("ReadModel,Command");

    static string Canonical(DotNetSourcePlacementSnapshot snapshot) => string.Join(
        '|',
        snapshot.Placements.Select(_ => $"{_.Artifact.Subject.Value}:{_.Artifact.Kind}:{_.Placement.Module}:{_.Placement.Slice}:{_.Placement.SliceKind}"));

    static DotNetSourcePlacementRequest Request(
        string subject,
        ArtifactKind artifactKind,
        GenerationSliceKind sliceKind,
        string placement)
    {
        var sourceSubject = new SubjectId { Value = $"dotnet://Banking/Banking/{subject}" };
        var segments = placement.Split('/');
        return new()
        {
            Artifact = new ArtifactKey { Subject = sourceSubject, Kind = artifactKind },
            Structure = new DotNetSourceStructure
            {
                Subject = sourceSubject,
                Project = "Banking",
                ProjectRole = DotNetProjectRole.Application,
                Namespace = $"Banking.{string.Join('.', segments)}",
                ProjectRelativePaths = [$"Source/{placement}/{segments[^1]}.cs"]
            },
            SliceKind = sliceKind,
            Policy = new DotNetSourceStructurePolicy
            {
                FeatureRoot = "Source",
                NamespaceSegmentsToSkip = 1
            }
        };
    }
}
