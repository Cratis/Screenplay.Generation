// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_identical_non_yielding_declaration_keeps_the_concept : given.yielding_concepts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var concept = Candidate("enum", true);
        var declaration = concept.OfType<ArtifactDeclarationFact>().Single();
        _result = Generate(
        [
            .. Command(), .. concept, .. UnboundMember("Permissions"),
            declaration with { Id = Id("retained-declaration"), Definition = declaration.Definition with { NameConflictYield = null } }
        ]);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_enum_concept() => _result.Source.ShouldContain("concept Permissions : Enum");
    [Fact] void should_keep_its_existing_binding() => _result.Source.ShouldContain("permissions Permissions");
    [Fact] void should_not_report_an_omission_warning_for_a_kept_concept() => _result.Diagnostics.Any(IsWarning).ShouldBeFalse();
}
