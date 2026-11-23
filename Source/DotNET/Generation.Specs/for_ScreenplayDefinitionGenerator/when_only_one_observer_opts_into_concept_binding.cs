// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_only_one_observer_opts_into_concept_binding : given.concept_only_type_uses
{
    GeneratedScreenplayDefinition _result;
    GeneratedScreenplayDefinition _reverse;
    ArtifactMemberTypeUseFact _foreign;

    void Because()
    {
        var own = OptedCommand();
        var observation = own.OfType<ArtifactMemberTypeUseFact>().Single();
        _foreign = observation with { Id = new() { Value = "foreign:type-use" }, Evidence = observation.Evidence with { Adapter = new() { Id = "foreign", Version = "1" } } };
        _result = GenerateAdapters(own, [_foreign]);
        _reverse = GenerateAdapters(own, [_foreign], true);
    }

    [Fact] void should_keep_the_foreign_observation_unchanged() => Projection(_result.AdapterRun.Facts.Single(record => record.Fact.Id == _foreign.Id).Fact).ShouldEqual(Projection(_foreign));
    [Fact] void should_not_waive_the_generic_missing_target_error() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).ShouldBeTrue();
    [Fact] void should_keep_the_other_observation_in_the_diagnostic() => _result.AdapterRun.Derivation.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).Facts.ShouldContain(_foreign.Id);
    [Fact] void should_not_manufacture_a_policy_for_the_other_producer() => _result.AdapterRun.Facts.Count(record => record.Fact is TypeUseBindingPolicyFact).ShouldEqual(1);
    [Fact] void should_not_invent_a_binding() => _result.AdapterRun.Derivation.Facts.Any(record => record.Fact is TypeUseBindingFact).ShouldBeFalse();
    [Fact] void should_keep_adapter_order_independent_source() => _reverse.Source.ShouldEqual(_result.Source);
    [Fact] void should_keep_adapter_order_independent_diagnostics() => Projection(_reverse.Diagnostics).ShouldEqual(Projection(_result.Diagnostics));
    [Fact] void should_keep_adapter_order_independent_lineage() => Projection(_reverse.AdapterRun.Derivation).ShouldEqual(Projection(_result.AdapterRun.Derivation));
}
