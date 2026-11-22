// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_joining_independent_artifact_usage_proofs : given.an_artifact_usage
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive(CompleteFacts());

    [Fact] void should_derive_the_exact_read_endpoints() => ((RelationshipFact)_result.Facts.Single().Fact).Definition.Key.ShouldEqual(new RelationshipKey { Kind = RelationshipKind.Reads, Source = Query.Subject, Target = Document.Subject });
    [Fact] void should_identify_the_stable_join_rule() => _result.Facts.Single().Lineage.Producer.ShouldEqual(new GenerationDerivationRuleIdentity { Id = "cratis.screenplay.artifact-usage", Version = "1.0.0" });
    [Fact] void should_retain_every_input_fact() => _result.Facts.Single().Lineage.Inputs.Select(id => id.Value).ShouldEqual("application:document", "application:entry", "application:query", "library:declaration", "library:usage");
    [Fact] void should_retain_evidence_for_every_input() => _result.Facts.Single().Lineage.Evidence.Length.ShouldEqual(5);
    [Fact] void should_report_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
