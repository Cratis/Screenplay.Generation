// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_conflicting_slice_kinds : for_GenerationResolver.given.facts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var eventArtifact = EventDefinition();
        var readModelSubject = new SubjectId { Value = "dotnet://Banking/ReadModels.Account" };
        var readModelArtifact = new ArtifactKey { Subject = readModelSubject, Kind = ArtifactKind.ReadModel };
        var eventEvidence = Fact("event", FirstAdapter).Evidence;
        var readModelEvidence = new Evidence
        {
            Adapter = SecondAdapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange
            {
                Path = "Accounts/Open/Account.cs",
                StartLine = 8,
                StartColumn = 1,
                EndLine = 8,
                EndColumn = 20
            }
        };

        _result = new ScreenplayDefinitionGenerator().Generate(
            [
                Contribution(
                    FirstAdapter,
                    new ArtifactFact
                    {
                        Id = new FactId { Value = "event" },
                        Subject = EventSubject,
                        Evidence = eventEvidence,
                        Definition = eventArtifact
                    },
                    Placement("event-placement", eventArtifact.Key, GenerationSliceKind.StateChange, eventEvidence)),
                Contribution(
                    SecondAdapter,
                    new ArtifactFact
                    {
                        Id = new FactId { Value = "read-model" },
                        Subject = readModelSubject,
                        Evidence = readModelEvidence,
                        Definition = new ArtifactDefinition
                        {
                            Key = readModelArtifact,
                            Name = "Account",
                            File = "Accounts/Open/Account.cs"
                        }
                    },
                    Placement("read-model-placement", readModelArtifact, GenerationSliceKind.StateView, readModelEvidence))
            ],
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_report_the_slice_conflict() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.ConflictingSliceKind);
    [Fact] void should_type_the_conflict_outcome() => _result.Diagnostics.Single().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Conflict);
    [Fact] void should_preserve_a_deterministic_subject() => _result.Diagnostics.Single().Subject.ShouldEqual(EventSubject);
    [Fact] void should_preserve_source_evidence() => _result.Diagnostics.Single().Source!.Path.ShouldEqual("Accounts/Open/AccountOpened.cs");
    [Fact] void should_not_emit_an_arbitrary_slice_role() => _result.Source.ShouldNotContain("slice Open");
    [Fact] void should_not_emit_artifacts_under_an_arbitrary_role() => _result.Source.ShouldNotContain("event AccountOpened");

    static ArtifactPlacementFact Placement(
        string id,
        ArtifactKey artifact,
        GenerationSliceKind kind,
        Evidence evidence) => new()
    {
        Id = new FactId { Value = id },
        Subject = artifact.Subject,
        Evidence = evidence,
        Artifact = artifact,
        Placement = new ArtifactPlacement
        {
            Module = "Accounts",
            Features = ["Opening"],
            Slice = "Open",
            SliceKind = kind
        }
    };
}
