// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_transferring_an_intentionally_omitted_target : given.omitted_input_members
{
    GeneratedScreenplayDefinition[] _results;
    GeneratedScreenplayDefinition _reverse;

    void Because()
    {
        TypeUseShapeKind[][] shapes =
        [
            [TypeUseShapeKind.Named],
            [TypeUseShapeKind.Optional, TypeUseShapeKind.Named],
            [TypeUseShapeKind.Collection, TypeUseShapeKind.Named],
            [TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named]
        ];
        _results = [.. shapes.Select(shape => Generate(OmittedFacts(shape)))];
        _reverse = Generate(OmittedFacts().Reverse());
    }

    [Fact] void should_generate_compiler_verified_screenplay() => _results.All(result => result.IsSuccess).ShouldBeTrue();
    [Fact] void should_preserve_the_authored_name() => _results.All(result => result.Source.Contains("filter Int", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_optional_shape() => _results[1].Source.ShouldContain("filter Int optional");
    [Fact] void should_preserve_collection_shape() => _results[2].Source.ShouldContain("filter Int[]");
    [Fact] void should_preserve_nested_shape() => _results[3].Source.ShouldContain("filter Int[] optional");
    [Fact] void should_report_the_original_warning_once() => _results.All(result => result.Diagnostics.Count(diagnostic => diagnostic.Code == Omission().Definition.Diagnostic.Code) == 1).ShouldBeTrue();
    [Fact] void should_preserve_the_warning_source() => _results[0].Diagnostics.Single(diagnostic => diagnostic.Code == Omission().Definition.Diagnostic.Code).Source.ShouldEqual(Omission().Evidence.Source);
    [Fact] void should_not_report_missing_target_or_incomplete_member() => _results.All(result => !result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget || diagnostic.Code == GenerationDiagnosticCodes.IncompleteArtifactMember)).ShouldBeTrue();
    [Fact] void should_not_invent_a_binding() => _results.All(result => !result.AdapterRun.Derivation.Facts.Any(record => record.Fact is TypeUseBindingFact)).ShouldBeTrue();
    [Fact] void should_preserve_the_exact_observation() => _results[0].AdapterRun.Derivation.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.ObservedTypeSubject.ShouldEqual(FlagSubject);
    [Fact] void should_keep_the_omission_as_provenance() => _results[0].AdapterRun.Facts.Single(record => record.Fact.Id == Omission().Id).Disposition.ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_include_the_original_omission_in_transferred_lineage() => _results[0].AdapterRun.Derivation.Facts.Single(record => record.Fact is TypeUseTargetOmittedFact).Lineage.Inputs.ShouldContain(Omission().Id);
    [Fact] void should_include_the_type_use_in_transferred_lineage() => _results[0].AdapterRun.Derivation.Facts.Single(record => record.Fact is TypeUseTargetOmittedFact).Lineage.Inputs.ShouldContain(FlagType().Id);
    [Fact] void should_be_order_independent() => _reverse.Source.ShouldEqual(_results[0].Source);
    [Fact] void should_keep_diagnostics_order_independent() => Projection(_reverse.Diagnostics).ShouldEqual(Projection(_results[0].Diagnostics));
    [Fact] void should_keep_lineage_order_independent() => Projection(_reverse.AdapterRun.Derivation).ShouldEqual(Projection(_results[0].AdapterRun.Derivation));
}
