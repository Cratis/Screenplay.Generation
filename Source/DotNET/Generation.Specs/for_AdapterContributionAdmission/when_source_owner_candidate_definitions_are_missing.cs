// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_source_owner_candidate_definitions_are_missing : given.source_owner_candidates
{
    AdapterContributionAdmissionResult _definition = null!;
    AdapterContributionAdmissionResult _policy = null!;
    void Because()
    {
        _definition = AdmitCandidate(Candidate() with { Definition = null! });
        _policy = AdmitCandidate(Candidate() with { Definition = Candidate().Definition with { Policy = null! } });
    }

    [Fact] void should_reject_missing_definitions_without_throwing() => _definition.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_missing_policy_without_throwing() => _policy.IsAdmitted.ShouldBeFalse();
}
