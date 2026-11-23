// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_yielding_concept_conflicts_with_a_normal_concept : given.yielding_concepts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because() => _result = Generate([.. Command(TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named), .. Candidate("enum", true), .. Candidate("vogen", false, true)]);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_normal_concept() => _result.Source.ShouldContain("concept Permissions : Int");
    [Fact] void should_drop_the_enum_concept() => _result.Source.ShouldNotContain("concept Permissions : Enum");
    [Fact] void should_keep_the_command_with_its_authored_shape() => _result.Source.ShouldContain("permissions Int[] optional");
    [Fact] void should_not_bind_the_command_to_the_winning_name() => Member(_result).Type.Subject.ShouldBeNull();
    [Fact] void should_preserve_the_observed_subject() => _result.AdapterRun.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.ObservedTypeSubject.ShouldEqual(Target);
    [Fact] void should_report_the_adapter_warning_once() => _result.Diagnostics.Count(IsWarning).ShouldEqual(1);
    [Fact] void should_keep_the_warning_source() => _result.Diagnostics.Single(IsWarning).Source!.Path.ShouldEqual("enum.cs");
    [Fact] void should_omit_the_declaration_with_diagnostic() => _result.AdapterRun.Facts.Single(record => record.Fact.Id == Id("concept-enum")).Disposition.ShouldEqual(GenerationFactDisposition.OmittedWithDiagnostic);
    [Fact] void should_not_report_missing_or_incomplete_targets() => _result.Diagnostics.Any(diagnostic => new[] { GenerationDiagnosticCodes.MissingTypeUseTarget, GenerationDiagnosticCodes.IncompleteArtifact, GenerationDiagnosticCodes.ConflictingConceptName }.Contains(diagnostic.Code, StringComparer.Ordinal)).ShouldBeFalse();
    [Fact] void should_retain_the_fallback_derivation_lineage() => _result.AdapterRun!.Derivation!.Facts.Single(record => record.Fact is TypeUseTargetOmittedFact).Lineage!.Inputs.ShouldContain(Id("concept-enum"));
}
