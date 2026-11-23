// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_all_same_named_concepts_yield : given.yielding_concepts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because() => _result = Generate([.. Command(), .. Candidate("enum", true), .. Candidate("second-enum", true)]);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_neither_concept() => _result.Source.ShouldNotContain("concept Permissions");
    [Fact] void should_keep_the_unbound_command() => _result.Source.ShouldContain("permissions Int");
    [Fact] void should_report_both_adapter_warnings() => _result.Diagnostics.Count(IsWarning).ShouldEqual(2);
    [Fact] void should_not_report_a_name_conflict() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(GenerationDiagnosticCodes.ConflictingConceptName);
}
