// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_diagnostic_projections_from_contributions : for_GenerationFactDerivation.given.diagnostic_projection
{
    GeneratedScreenplayDefinition _forward;
    GeneratedScreenplayDefinition _reverse;
    GenerationDiagnostic _ordinary;
    void Because()
    {
        _ordinary = Payload() with { Code = "OBS002" };
        AdapterContribution[] contributions =
        [
            new() { Adapter = Observer, Facts = [Observation(), Intent(), Support(), Classification()], Diagnostics = [_ordinary] },
            new() { Adapter = Entry().Evidence.Adapter, Facts = [Entry()] }
        ];
        var generator = new ScreenplayDefinitionGenerator();
        _forward = generator.Generate(contributions, new() { Domain = "Diagnostics" });
        _reverse = generator.Generate(contributions.Reverse().Select(contribution => contribution with { Facts = [.. contribution.Facts.Reverse()] }), new() { Domain = "Diagnostics" });
    }
    [Fact] void should_project_the_explicit_payload() => _forward.Diagnostics.Single(diagnostic => diagnostic.Code == Payload().Code).Subject.ShouldEqual(CommandSubject);
    [Fact] void should_keep_ordinary_method_diagnostics_unprojected() => JsonSerializer.Serialize(_forward.Diagnostics.Single(diagnostic => diagnostic.Code == _ordinary.Code)).ShouldEqual(JsonSerializer.Serialize(_ordinary));
    [Fact] void should_keep_ordinary_lineage_null() => _forward.Diagnostics.Single(diagnostic => diagnostic.Code == _ordinary.Code).Lineage.ShouldBeNull();
    [Fact] void should_have_identical_source_under_permutation() => _reverse.Source.ShouldEqual(_forward.Source);
    [Fact] void should_have_identical_diagnostics_under_permutation() => JsonSerializer.Serialize(_reverse.Diagnostics).ShouldEqual(JsonSerializer.Serialize(_forward.Diagnostics));
}
