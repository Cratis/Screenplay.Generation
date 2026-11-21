// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_artifact_usage_facets : given.artifact_usages
{
    AdapterContributionAdmissionResult _result;
    GenerationFact[] _facts;

    void Because()
    {
        _facts = [Declaration(), Entry(), Usage()];
        _result = Admit(contribution: Contribution(_facts));
    }

    [Fact] void should_admit_all_three_independent_capabilities() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_all_fact_envelopes() => _result.Snapshot.Facts.All(fact => !_facts.Any(input => ReferenceEquals(input, fact))).ShouldBeTrue();
    [Fact] void should_copy_nested_artifact_keys() => ReferenceEquals(_result.Snapshot.Facts.OfType<EntryPointFact>().Single().Definition.Artifact, ((EntryPointFact)_facts[1]).Definition.Artifact).ShouldBeFalse();
}
