// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_yielding_concept_has_no_name_conflict : given.yielding_concepts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because() => _result = Generate([.. Command(), .. Candidate("enum", true)]);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_concept() => _result.Source.ShouldContain("concept Permissions : Enum");
    [Fact] void should_bind_the_command() => Member(_result).Type.Subject.ShouldEqual(Target);
    [Fact] void should_not_report_the_adapter_warning() => _result.Diagnostics.Any(IsWarning).ShouldBeFalse();
    [Fact] void should_not_derive_an_omission() => _result.AdapterRun!.Derivation!.Facts.Any(record => record.Fact is TypeUseTargetOmittedFact).ShouldBeFalse();
}
