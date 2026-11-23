// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_serializing_source_owner_candidates : given.source_owner_candidates
{
    ArtifactSourcePlacementCandidateFact _copy = null!;
    void Because()
    {
        var admitted = AdmitCandidate(Candidate()).Snapshot.Facts.Single();
        _copy = JsonSerializer.Deserialize<ArtifactSourcePlacementCandidateFact>(JsonSerializer.Serialize(admitted, admitted.GetType()))!;
    }

    [Fact] void should_preserve_the_exact_owner() => _copy.Definition.Owner.ShouldEqual(Candidate().Definition.Owner);
    [Fact] void should_preserve_the_policy() => _copy.Definition.Policy.ShouldEqual(Candidate().Definition.Policy);
}
