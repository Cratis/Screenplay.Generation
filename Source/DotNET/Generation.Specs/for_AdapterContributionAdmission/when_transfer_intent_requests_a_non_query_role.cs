// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_transfer_intent_requests_a_non_query_role : given.declaration_input_members
{
    AdapterContributionAdmissionResult _result;

    void Because() => _result = AdmitEligibility(Transfer() with { Definition = Transfer().Definition with { Artifact = Transfer().Definition.Artifact with { Kind = ArtifactKind.Event } } });

    [Fact] void should_reject_the_unsupported_transfer_instead_of_expanding_its_meaning() => _result.IsAdmitted.ShouldBeFalse();
}
