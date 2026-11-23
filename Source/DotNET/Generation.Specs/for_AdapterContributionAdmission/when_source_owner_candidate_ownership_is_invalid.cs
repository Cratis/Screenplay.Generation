// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_source_owner_candidate_ownership_is_invalid : given.source_owner_candidates
{
    AdapterContributionAdmissionResult _result = null!;
    void Because() => _result = AdmitCandidate(Candidate() with { Definition = Candidate().Definition with { Owner = ArtifactKey(ExternalSubject, ArtifactKind.Command) } });

    [Fact] void should_require_exact_self_ownership() => _result.IsAdmitted.ShouldBeFalse();
}
