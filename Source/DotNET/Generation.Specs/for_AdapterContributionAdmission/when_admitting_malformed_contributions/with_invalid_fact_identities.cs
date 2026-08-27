// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.when_admitting_malformed_contributions;

public class with_invalid_fact_identities : given.a_contribution
{
    AdapterContributionAdmissionResult _duplicate = null!;
    AdapterContributionAdmissionResult _empty = null!;
    AdapterContributionAdmissionResult _unscoped = null!;

    void Because()
    {
        var duplicateFacts = EveryFact();
        duplicateFacts[1] = duplicateFacts[1] with { Id = duplicateFacts[0].Id };
        _duplicate = Admit(contribution: Contribution(duplicateFacts));

        var emptyFacts = EveryFact();
        emptyFacts[0] = emptyFacts[0] with { Id = new FactId { Value = string.Empty } };
        _empty = Admit(contribution: Contribution(emptyFacts));

        var unscopedFacts = EveryFact();
        unscopedFacts[0] = unscopedFacts[0] with { Id = new FactId { Value = "legacy-artifact" } };
        _unscoped = Admit(contribution: Contribution(unscopedFacts));
    }

    [Fact] void should_reject_duplicate_fact_identities_atomically() => AssertRejected(_duplicate, AdapterContributionAdmissionDiagnosticCode.DuplicateFactId);
    [Fact] void should_reject_empty_fact_identities_atomically() => AssertRejected(_empty, AdapterContributionAdmissionDiagnosticCode.InvalidFactId);
    [Fact] void should_reject_unscoped_fact_identities_without_rewriting_them() => AssertRejected(_unscoped, AdapterContributionAdmissionDiagnosticCode.UnscopedFactId);

    static void AssertRejected(AdapterContributionAdmissionResult result, AdapterContributionAdmissionDiagnosticCode code)
    {
        result.Snapshot.ShouldBeNull();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);
    }
}
