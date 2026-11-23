// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_source_owner_candidate_policy_is_unsupported : given.source_owner_candidates
{
    AdapterContributionAdmissionResult _result = null!;
    void Because() => _result = AdmitCandidate(Candidate() with { Definition = Candidate().Definition with { Policy = new() { Version = 2 } } });

    [Fact] void should_reject_an_unknown_version() => _result.IsAdmitted.ShouldBeFalse();
}
