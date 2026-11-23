// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_source_owner_candidate_capability_is_not_declared : given.source_owner_candidates
{
    AdapterContributionAdmissionResult _result = null!;
    void Because() => _result = Admit(descriptor: Descriptor([.. AllCapabilities().Where(capability => capability != GenerationFactCapability.ArtifactSourcePlacementCandidate)]), contribution: Contribution([Candidate()]));

    [Fact] void should_reject_the_contribution() => _result.IsAdmitted.ShouldBeFalse();
}
