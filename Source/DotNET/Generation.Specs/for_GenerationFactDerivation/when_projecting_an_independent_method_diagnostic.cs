// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_projecting_an_independent_method_diagnostic : given.diagnostic_projection
{
    GenerationDerivationSnapshot _result;
    GenerationDiagnostic _diagnostic;
    GenerationDiagnostic _roundTrip;

    void Because()
    {
        _result = Derive(Inputs());
        _diagnostic = Projected(_result);
        _roundTrip = JsonSerializer.Deserialize<GenerationDiagnostic>(JsonSerializer.Serialize(_diagnostic))!;
    }

    [Fact] void should_attribute_to_the_exact_independent_entry_point() => _diagnostic.Subject.ShouldEqual(CommandSubject);
    [Fact] void should_preserve_the_invocation_location() => _diagnostic.Source.ShouldEqual(Invocation);
    [Fact] void should_preserve_local_payload_supports() => _diagnostic.Facts.SequenceEqual(Payload().Facts).ShouldBeTrue();
    [Fact] void should_preserve_payload_text_and_outcome() => (_diagnostic.Code, _diagnostic.Message, _diagnostic.Severity, _diagnostic.Outcome).ShouldEqual((Payload().Code, Payload().Message, Payload().Severity, Payload().Outcome));
    [Fact] void should_emit_no_facts() => _result.Rules.Single(rule => rule.Rule.Id == "cratis.screenplay.method-diagnostic-projection").Outputs.ShouldBeEmpty();
    [Fact] void should_retain_the_original_producer() => _diagnostic.Lineage!.Evidence.Any(evidence => evidence.Adapter == Observer && evidence.Source == Invocation).ShouldBeTrue();
    [Fact] void should_include_the_entry_and_eligibility_evidence() => _diagnostic.Lineage!.Inputs.ShouldContainOnly([.. Inputs().Where(fact => fact is not ArtifactDeclarationFact || fact.Id == Observation().Id).Select(fact => fact.Id)]);
    [Fact] void should_round_trip_concrete_diagnostic_lineage() => JsonSerializer.Serialize(_roundTrip).ShouldEqual(JsonSerializer.Serialize(_diagnostic));
}
