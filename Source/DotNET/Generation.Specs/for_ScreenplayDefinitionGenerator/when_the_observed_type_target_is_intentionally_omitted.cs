// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_the_observed_type_target_is_intentionally_omitted : given.an_omitted_type_use
{
    GeneratedScreenplayDefinition[] _results = [];
    GeneratedScreenplayDefinition _permuted = null!;

    void Because()
    {
        TypeUseShapeKind[][] shapes =
        [
            [TypeUseShapeKind.Named],
            [TypeUseShapeKind.Optional, TypeUseShapeKind.Named],
            [TypeUseShapeKind.Collection, TypeUseShapeKind.Named],
            [TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named]
        ];
        _results = [.. shapes.Select(shape => Generate(Facts(shape)))];
        _permuted = Generate(Facts().Reverse());
    }

    [Fact] void should_generate_and_compiler_verify_every_supported_shape() => _results.All(result => result.IsSuccess).ShouldBeTrue();
    [Fact] void should_retain_the_command_and_member() => _results.All(result => result.Graph.Artifacts.Single().Variants.Single().Definition.Properties.Single().Name == "permissions").ShouldBeTrue();
    [Fact] void should_leave_the_member_unbound() => _results.All(result => result.Graph.Artifacts.Single().Variants.Single().Definition.Properties.Single().Type.Subject is null).ShouldBeTrue();
    [Fact] void should_keep_the_terminal_name() => _results.All(result => result.Graph.Artifacts.Single().Variants.Single().Definition.Properties.Single().Type.Name == "Int").ShouldBeTrue();
    [Fact] void should_preserve_the_exact_observation() => _results.All(result => result.AdapterRun.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.ObservedTypeSubject == Target).ShouldBeTrue();
    [Fact] void should_report_the_adapter_warning_once() => _results.All(result => result.Diagnostics.Count(diagnostic => diagnostic.Code == "example.FlagsEnumOmitted") == 1).ShouldBeTrue();
    [Fact] void should_retain_the_warning_location() => _results.All(result => result.Diagnostics.Single(diagnostic => diagnostic.Code == "example.FlagsEnumOmitted").Source?.StartLine == 3).ShouldBeTrue();
    [Fact] void should_not_report_missing_or_incomplete_members() => _results.All(result => !result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget || diagnostic.Code == GenerationDiagnosticCodes.IncompleteArtifactMember)).ShouldBeTrue();
    [Fact] void should_not_leave_unknown_dispositions() => _results.All(result => result.AdapterRun.Facts.Concat(result.AdapterRun.Derivation.Facts).All(record => record.Disposition != GenerationFactDisposition.Unknown)).ShouldBeTrue();
    [Fact] void should_not_derive_a_binding() => _results.All(result => !result.AdapterRun.Derivation.Facts.Any(record => record.Fact is TypeUseBindingFact)).ShouldBeTrue();
    [Fact] void should_preserve_optional_shape() => _results[1].Graph.Artifacts.Single().Variants.Single().Definition.Properties.Single().Type.IsOptional.ShouldBeTrue();
    [Fact] void should_preserve_collection_shape() => _results[2].Graph.Artifacts.Single().Variants.Single().Definition.Properties.Single().Type.IsCollection.ShouldBeTrue();
    [Fact] void should_preserve_nested_shape() => _results[3].Source.ShouldContain("permissions Int[] optional");
    [Fact] void should_be_deterministic_under_fact_permutation() => _permuted.Source.ShouldEqual(_results[0].Source);
    [Fact] void should_keep_diagnostics_deterministic_under_fact_permutation() => _permuted.Diagnostics.Select(DiagnosticValue).ShouldEqual(_results[0].Diagnostics.Select(DiagnosticValue));

    static object DiagnosticValue(GenerationDiagnostic diagnostic) =>
        (diagnostic.Code, diagnostic.Severity, diagnostic.Message, diagnostic.Source, string.Join(',', diagnostic.Facts.Select(fact => fact.Value)));
}
