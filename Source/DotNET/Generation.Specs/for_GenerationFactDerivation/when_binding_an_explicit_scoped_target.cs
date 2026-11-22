// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_binding_an_explicit_scoped_target : given.scoped_type_uses
{
    GenerationDerivationSnapshot _result;
    GenerationDerivationSnapshot _reversed;

    void Because()
    {
        _result = Derive(Facts(Second));
        _reversed = Derive(Facts(Second).Reverse());
    }

    [Fact] void should_not_inherit_the_owning_store() => ((TypeUseBindingFact)_result.Facts.Single().Fact).Definition.Target.Scope.ShouldEqual(Second);
    [Fact] void should_preserve_the_shared_observed_clr_subject() => ((TypeUseBindingFact)_result.Facts.Single().Fact).Definition.Target.Subject.ShouldEqual(ModelSubject);
    [Fact] void should_make_derived_ids_stable_across_input_order() => _reversed.Facts.Single().Fact.Id.ShouldEqual(_result.Facts.Single().Fact.Id);
    [Fact] void should_retain_both_scope_declarations_as_lineage_proofs() => _result.Facts.Single().Lineage.Inputs.Intersect([Id("first-scope"), Id("second-scope")]).Count().ShouldEqual(2);
    [Fact] void should_report_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
