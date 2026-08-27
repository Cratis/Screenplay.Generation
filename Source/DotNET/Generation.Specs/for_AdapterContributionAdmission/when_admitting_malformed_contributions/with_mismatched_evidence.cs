// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.when_admitting_malformed_contributions;

public class with_mismatched_evidence : given.a_contribution
{
    AdapterContributionAdmissionResult _result = null!;

    void Because()
    {
        var facts = EveryFact();
        facts[0] = facts[0] with
        {
            Evidence = Evidence() with
            {
                Adapter = new AdapterIdentity { Id = "other", Version = "1.2.3" }
            }
        };
        _result = Admit(contribution: Contribution(facts));
    }

    [Fact] void should_reject_the_whole_contribution() => _result.Snapshot.ShouldBeNull();
    [Fact] void should_report_the_typed_evidence_mismatch() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(AdapterContributionAdmissionDiagnosticCode.EvidenceAdapterMismatch);
}
