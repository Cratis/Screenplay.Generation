// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_source_owner_candidates : given.source_owner_candidates
{
    ArtifactSourcePlacementCandidateFact _input = null!;
    AdapterContributionAdmissionResult _result = null!;
    void Because()
    {
        _input = Candidate();
        _result = AdmitCandidate(_input);
    }

    [Fact] void should_admit_the_versioned_candidate() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_the_policy() => ReferenceEquals(_input.Definition.Policy, _result.Snapshot.Facts.OfType<ArtifactSourcePlacementCandidateFact>().Single().Definition.Policy).ShouldBeFalse();
    [Fact] void should_freeze_the_owner_key() => ReferenceEquals(_input.Definition.Owner, _result.Snapshot.Facts.OfType<ArtifactSourcePlacementCandidateFact>().Single().Definition.Owner).ShouldBeFalse();
}
