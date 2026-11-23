// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_yielding_concept_has_other_structural_conflicts : given.yielding_concepts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var facts = Command();
        var typeUse = facts.OfType<ArtifactMemberTypeUseFact>().Single();
        _result = Generate(
        [
            .. facts, .. Candidate("enum", true), .. Candidate("vogen", false, true),
            typeUse with { Id = Id("conflicting-type-use"), Definition = typeUse.Definition with { Type = typeUse.Definition.Type with { Name = "String" } } }
        ]);
    }

    [Fact] void should_not_hide_structural_conflicts() => _result.Diagnostics.Any(diagnostic => diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict).ShouldBeTrue();
    [Fact] void should_not_generate_the_conflicted_command() => _result.Source.ShouldNotContain("command SetPermissions");
    [Fact] void should_still_report_the_adapter_warning_once() => _result.Diagnostics.Count(IsWarning).ShouldEqual(1);
}
