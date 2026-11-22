// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_support_is_malformed_in_a_legacy_contribution : given.method_facts
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts(), Support() with { Definition = Support().Definition with { Alternatives = [] } }]);

    [Fact] void should_not_promote_the_candidate_to_unconditional() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_locate_the_invalid_metadata() => _result.Diagnostics.Single().Source.ShouldEqual(UsageEvidence().Source);
    [Fact] void should_report_unknown_not_conflict() => _result.Diagnostics.Single().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Unknown);
}
