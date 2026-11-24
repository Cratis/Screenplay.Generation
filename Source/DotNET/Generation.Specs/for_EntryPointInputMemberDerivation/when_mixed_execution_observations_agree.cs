// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_mixed_execution_observations_agree : given.input_only_execution
{
    GenerationDerivationSnapshot _forward;
    GenerationDerivationSnapshot _reverse;

    void Because()
    {
        var facts = InputOnlyFacts(Execution());
        _forward = Derive(facts);
        _reverse = Derive([.. facts.Reverse()]);
    }

    [Fact] void should_transfer_the_member_once() => Members(_forward).Length.ShouldEqual(1);
    [Fact] void should_retain_input_only_execution_proof() => _forward.Facts.Single(record => record.Fact is ArtifactMemberDeclarationFact).Lineage.Inputs.ShouldContain(InputExecution().Id);
    [Fact] void should_retain_artifact_usage_execution_proof() => _forward.Facts.Single(record => record.Fact is ArtifactMemberDeclarationFact).Lineage.Inputs.ShouldContain(Execution().Id);
    [Fact] void should_not_depend_on_fact_order() => Projection(_forward).ShouldEqual(Projection(_reverse));
}
