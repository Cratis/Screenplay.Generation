// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_effective_usage_endpoints_conflict : given.a_granular_artifact_usage
{
    GenerationDerivationSnapshot[] _results;

    void Because() => _results =
    [
        Derive([.. CompleteFacts(), .. EndpointFacts(Query, "FindCustomer", conflict: true)]),
        Derive([.. CompleteFacts(), .. EndpointFacts(Document, "Customer", conflict: true)])
    ];

    [Fact] void should_not_derive_reads_for_either_conflicting_endpoint() => _results.All(result => result.Facts.IsEmpty).ShouldBeTrue();
    [Fact] void should_report_gen0057_for_each_effective_conflict() => _results.All(result => result.Diagnostics.Single().Code == GenerationDiagnosticCodes.ConflictingArtifactUsageMapping).ShouldBeTrue();
    [Fact] void should_classify_each_diagnostic_as_a_conflict() => _results.All(result => result.Diagnostics.Single().Outcome == GenerationDiagnosticOutcome.Conflict).ShouldBeTrue();
}
