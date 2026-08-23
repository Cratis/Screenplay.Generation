// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_an_unplaced_unsupported_artifact : for_GenerationResolver.given.facts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Handlers.OpenAccount" };
        _result = new ScreenplayDefinitionGenerator().Generate(
            [
                Contribution(FirstAdapter, new ArtifactFact
                {
                    Id = new FactId { Value = "handler" },
                    Subject = subject,
                    Evidence = new Evidence
                    {
                        Adapter = FirstAdapter,
                        Strength = EvidenceStrength.Exact,
                        Source = new SourceRange
                        {
                            Path = "Accounts/Open/OpenAccountHandler.cs",
                            StartLine = 5,
                            StartColumn = 1,
                            EndLine = 5,
                            EndColumn = 25
                        }
                    },
                    Definition = new ArtifactDefinition
                    {
                        Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Handler },
                        Name = "OpenAccountHandler"
                    }
                })
            ],
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_report_the_unsupported_artifact() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.UnsupportedArtifact);
    [Fact] void should_type_the_unsupported_outcome() => _result.Diagnostics.Single().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Unsupported);
    [Fact] void should_report_the_subject() => _result.Diagnostics.Single().Subject.ShouldEqual(new SubjectId { Value = "dotnet://Banking/Handlers.OpenAccount" });
    [Fact] void should_preserve_the_source() => _result.Diagnostics.Single().Source!.Path.ShouldEqual("Accounts/Open/OpenAccountHandler.cs");
    [Fact] void should_not_emit_the_artifact() => _result.Source.ShouldNotContain("OpenAccountHandler");
}
