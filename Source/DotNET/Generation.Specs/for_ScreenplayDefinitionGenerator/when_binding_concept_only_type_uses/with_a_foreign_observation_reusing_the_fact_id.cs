// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.when_binding_concept_only_type_uses;

public class with_a_foreign_observation_reusing_the_fact_id : given.concept_only_type_uses
{
    GenerationFact[] _own;
    ArtifactMemberTypeUseFact _foreign;
    GeneratedScreenplayDefinition _result;

    void Establish()
    {
        _own = OptedCommand();
        var observation = _own.OfType<ArtifactMemberTypeUseFact>().Single();
        _foreign = observation with { Evidence = observation.Evidence with { Adapter = new() { Id = "foreign", Version = "1" } } };
    }

    void Because() => _result = GenerateAdapters(_own, [_foreign]);

    [Fact] void should_keep_both_observations() => _result.AdapterRun.Facts.Count(record => record.Fact is ArtifactMemberTypeUseFact && record.Fact.Id == _foreign.Id).ShouldEqual(2);
    [Fact] void should_keep_the_foreign_observation_unchanged() => Projection(_result.AdapterRun.Facts.Single(record => record.Fact is ArtifactMemberTypeUseFact && record.Fact.Evidence.Adapter == _foreign.Evidence.Adapter).Fact).ShouldEqual(Projection(_foreign));
    [Fact] void should_keep_the_strict_missing_target_error() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).ShouldBeTrue();
    [Fact] void should_link_the_co_observed_identity_in_the_missing_target_error() => _result.AdapterRun.Derivation.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).Facts.ShouldContain(_foreign.Id);
    [Fact] void should_not_manufacture_a_foreign_policy() => _result.AdapterRun.Facts.Count(record => record.Fact is TypeUseBindingPolicyFact).ShouldEqual(1);
    [Fact] void should_not_invent_a_binding() => _result.AdapterRun.Derivation.Facts.Any(record => record.Fact is TypeUseBindingFact).ShouldBeFalse();
}
