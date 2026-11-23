// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.given;

public class method_eligibility : artifact_usages
{
    protected static MethodExecutionClassificationFact Classification() => new()
    {
        Id = Id("classification"),
        Subject = ArtifactSubject,
        Evidence = Evidence(),
        Definition = new MethodExecutionClassificationDefinition { Method = ArtifactSubject, Kind = MethodExecutionKind.Ordinary }
    };

    protected static MethodFactSupportFact Support(IReadOnlyList<MethodFactSupportAlternative>? alternatives = null) => new()
    {
        Id = Id("support"),
        Subject = ArtifactSubject,
        Evidence = Evidence(),
        Definition = new MethodFactSupportDefinition
        {
            Candidate = Usage().Id,
            Alternatives = alternatives ?? [new MethodFactSupportAlternative { Method = ArtifactSubject }]
        }
    };

    protected static AdapterContributionAdmissionResult AdmitEligibility(params GenerationFact[] facts) => Admit(
        descriptor: Descriptor([.. Enum.GetValues<GenerationFactCapability>().Where(capability => capability != GenerationFactCapability.Unknown)]),
        contribution: Contribution(facts));
}
