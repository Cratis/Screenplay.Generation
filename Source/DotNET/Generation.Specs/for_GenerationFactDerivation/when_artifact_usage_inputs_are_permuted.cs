// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_artifact_usage_inputs_are_permuted : given.an_artifact_usage
{
    GenerationDerivationSnapshot _forward;
    GenerationDerivationSnapshot _reverse;

    void Because()
    {
        GenerationFact[] facts = [.. CompleteFacts(), Usage("duplicate-usage"), Entry("duplicate-entry"), UsageDeclaration("duplicate-declaration")];
        _forward = Derive(facts);
        _reverse = Derive([.. facts.Reverse()]);
    }

    [Fact] void should_have_identical_records_diagnostics_and_rule_inputs() => Projection(_reverse).ShouldEqual(Projection(_forward));
    [Fact] void should_consolidate_duplicate_agreeing_proofs() => _forward.Facts.Length.ShouldEqual(1);
    [Fact] void should_keep_all_agreeing_lineage() => _forward.Facts.Single().Lineage.Inputs.Length.ShouldEqual(8);
}
