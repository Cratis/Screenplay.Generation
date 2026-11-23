// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_coalescing_disjoint_omission_groups : given.omission_groups
{
    GeneratedScreenplayDefinition _result;
    void Because()
    {
        var first = Member("first");
        var second = Member("second");
        var unrelated = Member("unrelated");
        var secondGroup = Group(second) with { Id = Id("second-group") };
        _result = Generate([first, second, unrelated, Group(first), secondGroup]);
    }
    [Fact] void should_keep_each_explanation_separate_even_with_the_same_subject() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == "OBS_GROUP").ShouldEqual(2);
    [Fact] void should_keep_uncovered_generic_noise() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.OmittedGenerationFact).Facts.Single().ShouldEqual(Id("unrelated"));
    [Fact] void should_not_share_covered_lineage_between_groups() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == "OBS_GROUP").All(diagnostic => diagnostic.Facts.Contains(Id("first")) != diagnostic.Facts.Contains(Id("second"))).ShouldBeTrue();
}
