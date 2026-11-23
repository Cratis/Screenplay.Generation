// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_non_yielding_concept_matches_an_unbound_type_name : given.yielding_concepts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because() => _result = Generate([.. Command(), .. Candidate("enum", false), .. UnboundMember("Permissions")]);

    [Fact] void should_keep_the_non_yielding_concept() => _result.Source.ShouldContain("concept Permissions : Enum");
    [Fact] void should_keep_its_existing_binding() => _result.Source.ShouldContain("permissions Permissions");
    [Fact] void should_not_report_a_yielding_warning() => _result.Diagnostics.Any(IsWarning).ShouldBeFalse();
}
