// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_cross_adapter_subject_references : given.a_contribution
{
    AdapterContributionAdmissionResult _result = null!;

    void Because()
    {
        var facts = EveryFact();
        _result = Admit(
            Descriptor(GenerationFactCapability.Artifact, GenerationFactCapability.Relationship),
            Contribution([facts[0], facts[2]]));
    }

    [Fact] void should_admit_structurally_valid_references_not_declared_by_this_adapter() => _result.IsAdmitted.ShouldBeTrue();
}
