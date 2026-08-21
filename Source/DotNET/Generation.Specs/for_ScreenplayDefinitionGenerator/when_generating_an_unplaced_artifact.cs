// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_an_unplaced_artifact : for_GenerationResolver.given.facts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because() => _result = new ScreenplayDefinitionGenerator().Generate(
        [Contribution(FirstAdapter, Fact("event", FirstAdapter))],
        new ScreenplayGenerationOptions { Domain = "Banking" });

    [Fact] void should_report_the_omission() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.IncompleteArtifact);
    [Fact] void should_not_emit_the_event() => _result.Source.ShouldNotContain("event AccountOpened");
}
