// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_legacy_and_granular_usage_endpoints_reconcile : given.a_granular_artifact_usage
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts(), .. EndpointFacts(Query, "FindCustomer"), .. EndpointFacts(Document, "Customer")]);

    [Fact] void should_derive_exactly_one_reads_edge() => ((RelationshipFact)_result.Facts.Single().Fact).Definition.Key.Kind.ShouldEqual(RelationshipKind.Reads);
    [Fact] void should_not_report_a_usage_conflict() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_retain_the_reconciliation_proofs() => _result.Facts.Single().Lineage.Inputs.Select(id => id.Value).ShouldContain("application:Customer-member");
}
