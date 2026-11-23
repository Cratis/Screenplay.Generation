// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_multiple_producers_report_the_same_concept_yield : given.co_owned_yielding_concepts
{
    GeneratedScreenplayDefinition _two = null!;
    GeneratedScreenplayDefinition _three = null!;
    GenerationFact[] _contributors = null!;

    void Because()
    {
        var first = CoOwner("first");
        var second = CoOwner("second");
        var third = CoOwner("third");
        _contributors = [.. first, .. second, .. third];
        _two = GenerateCoOwners(first, second);
        _three = GenerateCoOwners(first, second, third);
    }

    [Fact] void should_succeed_with_two_co_owners() => _two.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_with_three_co_owners() => _three.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_one_warning_for_two_co_owners() => _two.Diagnostics.Count(IsWarning).ShouldEqual(1);
    [Fact] void should_report_one_warning_for_three_co_owners() => _three.Diagnostics.Count(IsWarning).ShouldEqual(1);
    [Fact] void should_preserve_the_primary_source_location() => Diagnostic.Source.ShouldEqual(_contributors[0].Evidence.Source);
    [Fact] void should_union_all_declaration_and_support_ids() => Diagnostic.Facts.ShouldEqual(_contributors.Select(fact => fact.Id).OrderBy(id => id.Value, StringComparer.Ordinal));
    [Fact] void should_retain_all_contributors_in_lineage() => Diagnostic.Lineage!.Inputs.SequenceEqual(Diagnostic.Facts).ShouldBeTrue();
    [Fact] void should_retain_evidence_aligned_with_each_lineage_input() => Diagnostic.Lineage!.Evidence.ShouldEqual(_contributors.OrderBy(fact => fact.Id.Value, StringComparer.Ordinal).Select(fact => fact.Evidence));
    [Fact] void should_keep_the_command_unbound() => Member(_three).Type.Subject.ShouldBeNull();
    [Fact] void should_keep_the_same_generated_source() => _three.Source.ShouldEqual(_two.Source);
    [Fact] void should_retain_all_original_fact_ids() => _contributors.Select(fact => fact.Id).Except(_three.AdapterRun!.Facts.Select(record => record.Fact.Id)).ShouldBeEmpty();
    [Fact] void should_omit_every_contributing_declaration() => _three.AdapterRun!.Facts.Where(record => record.Fact is ArtifactDeclarationFact && Diagnostic.Facts.Contains(record.Fact.Id)).All(record => record.Disposition == GenerationFactDisposition.OmittedWithDiagnostic).ShouldBeTrue();
    [Fact] void should_record_one_rule_diagnostic() => YieldRule.Diagnostics.Length.ShouldEqual(1);
    [Fact] void should_record_every_support_in_the_rule_inputs() => Diagnostic.Facts.Except(YieldRule.Inputs).ShouldBeEmpty();
    [Fact] void should_reuse_the_coalesced_diagnostic_for_fallbacks() => _three.AdapterRun!.Derivation.Facts.Select(record => record.Fact).OfType<TypeUseTargetOmittedFact>().Select(fact => AdapterRunProjection(fact.Definition.Diagnostic)).Distinct().ShouldEqual([AdapterRunProjection(Diagnostic)]);

    GenerationDiagnostic Diagnostic => _three.Diagnostics.Single(IsWarning);
    GenerationDerivationRuleRecord YieldRule => _three.AdapterRun!.Derivation.Rules.Single(rule => rule.Rule.Id == "cratis.screenplay.concept-name-yield");
}
