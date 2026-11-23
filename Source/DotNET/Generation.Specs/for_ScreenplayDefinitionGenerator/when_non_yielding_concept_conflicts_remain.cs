// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_non_yielding_concept_conflicts_remain : given.yielding_concepts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because() => _result = Generate([.. Command(), .. Candidate("enum", true), .. Candidate("vogen", false, true), .. Candidate("other-vogen", false, true)]);

    [Fact] void should_retain_the_existing_name_conflict() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingConceptName);
    [Fact] void should_still_warn_about_the_yielded_concept() => _result.Diagnostics.Count(IsWarning).ShouldEqual(1);
    [Fact] void should_keep_the_command_fallback() => _result.Source.ShouldContain("permissions Int");
}
