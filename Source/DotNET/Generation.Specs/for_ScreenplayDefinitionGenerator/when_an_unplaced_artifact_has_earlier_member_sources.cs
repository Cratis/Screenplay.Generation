// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_unplaced_artifact_has_earlier_member_sources : given.artifact_diagnostic_sources
{
    GeneratedScreenplayDefinition _result;

    void Because()
    {
        var command = Document with { Kind = ArtifactKind.Command };
        _result = Generate(Contribution([Declaration("command", command), .. Members("command", command)]));
    }

    [Fact] void should_report_the_artifact_declaration_source() => Omissions(_result).Single().Source.ShouldEqual(At(5, 83, 105).Source);
    [Fact] void should_keep_the_incomplete_artifact_code() => Omissions(_result).Single().Code.ShouldEqual(GenerationDiagnosticCodes.IncompleteArtifact);
    [Fact] void should_keep_the_warning_severity() => Omissions(_result).Single().Severity.ShouldEqual(GenerationDiagnosticSeverity.Warning);
    [Fact] void should_keep_the_unknown_outcome() => Omissions(_result).Single().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Unknown);
    [Fact] void should_not_emit_the_unplaced_command() => _result.Source.ShouldNotContain("Student");
}
