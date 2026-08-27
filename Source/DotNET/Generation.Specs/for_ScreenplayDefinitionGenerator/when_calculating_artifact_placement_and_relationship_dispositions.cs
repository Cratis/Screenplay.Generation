// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_calculating_artifact_placement_and_relationship_dispositions : given.a_generator
{
    AdapterRunSnapshot _input = null!;
    AdapterRunSnapshot _copiedAfterGeneration = null!;
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var emitted = Event("AccountOpened", "Open");
        var artifact = (ArtifactFact)emitted[0];
        var effectivePlacement = (ArtifactPlacementFact)emitted[1];
        var weakerPlacement = effectivePlacement with
        {
            Id = new FactId { Value = "placement:AccountOpened:weaker" },
            Placement = effectivePlacement.Placement with { Slice = "Events" },
            Evidence = effectivePlacement.Evidence with { Strength = EvidenceStrength.Heuristic }
        };
        var duplicateArtifact = artifact with { Id = new FactId { Value = "event:AccountOpened:duplicate" } };
        var unsupportedSubject = new SubjectId { Value = "dotnet://Banking/Handlers.OpenAccount" };
        var unsupportedKey = new ArtifactKey { Subject = unsupportedSubject, Kind = ArtifactKind.Handler };
        var unsupportedArtifact = new ArtifactFact
        {
            Id = new FactId { Value = "handler:unsupported" },
            Subject = unsupportedSubject,
            Evidence = Evidence("Handlers/OpenAccount.cs"),
            Definition = new ArtifactDefinition { Key = unsupportedKey, Name = "OpenAccountHandler" }
        };
        var unsupportedPlacement = new ArtifactPlacementFact
        {
            Id = new FactId { Value = "handler:unsupported:placement" },
            Subject = unsupportedSubject,
            Evidence = Evidence("Handlers/OpenAccount.cs"),
            Artifact = unsupportedKey,
            Placement = effectivePlacement.Placement with { Slice = "Handle" }
        };
        var unplacedSubject = new SubjectId { Value = "dotnet://Banking/Events.AccountClosed" };
        var unplaced = new ArtifactFact
        {
            Id = new FactId { Value = "event:unplaced" },
            Subject = unplacedSubject,
            Evidence = Evidence("Events/AccountClosed.cs"),
            Definition = new ArtifactDefinition
            {
                Key = new ArtifactKey { Subject = unplacedSubject, Kind = ArtifactKind.Event },
                Name = "AccountClosed"
            }
        };
        var commandSubject = new SubjectId { Value = "dotnet://Banking/Commands.OpenAccount" };
        var commandKey = new ArtifactKey { Subject = commandSubject, Kind = ArtifactKind.Command };
        var commandArtifact = new ArtifactFact
        {
            Id = new FactId { Value = "command:open" },
            Subject = commandSubject,
            Evidence = Evidence("Commands/OpenAccount.cs"),
            Definition = new ArtifactDefinition { Key = commandKey, Name = "OpenAccount" }
        };
        var commandPlacement = new ArtifactPlacementFact
        {
            Id = new FactId { Value = "command:open:placement" },
            Subject = commandSubject,
            Evidence = Evidence("Commands/OpenAccount.cs"),
            Artifact = commandKey,
            Placement = effectivePlacement.Placement
        };
        var loweredRelationship = new RelationshipFact
        {
            Id = new FactId { Value = "relationship:produces" },
            Subject = commandSubject,
            Evidence = Evidence("Commands/OpenAccount.cs"),
            Definition = new RelationshipDefinition
            {
                Key = new RelationshipKey
                {
                    Kind = RelationshipKind.Produces,
                    Source = commandSubject,
                    Target = artifact.Subject
                }
            }
        };
        var unsupportedRelationship = new RelationshipFact
        {
            Id = new FactId { Value = "relationship:handles" },
            Subject = artifact.Subject,
            Evidence = Evidence("Handlers/OpenAccount.cs"),
            Definition = new RelationshipDefinition
            {
                Key = new RelationshipKey
                {
                    Kind = RelationshipKind.Handles,
                    Source = artifact.Subject,
                    Target = unsupportedSubject
                }
            }
        };
        _input = Snapshot(Completed(
            Adapter,
            [
                artifact,
                effectivePlacement,
                weakerPlacement,
                duplicateArtifact,
                unsupportedArtifact,
                unsupportedPlacement,
                unplaced,
                commandArtifact,
                commandPlacement,
                loweredRelationship,
                unsupportedRelationship
            ]));

        _result = Generator.Generate(_input, new ScreenplayGenerationOptions { Domain = "Banking" });
        _copiedAfterGeneration = _input with { Adapters = [], Facts = [], Diagnostics = [] };
    }

    [Fact] void should_lower_both_equivalent_artifact_assertions() => Dispositions("event:AccountOpened", "event:AccountOpened:duplicate").ShouldContainOnly(GenerationFactDisposition.Lowered, GenerationFactDisposition.Lowered);
    [Fact] void should_lower_only_the_effective_placement() => Disposition("placement:AccountOpened").ShouldEqual(GenerationFactDisposition.Lowered);
    [Fact] void should_retain_the_weaker_placement_as_provenance() => Disposition("placement:AccountOpened:weaker").ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_omit_the_unsupported_artifact_with_its_stable_diagnostic() => OmittedCode("handler:unsupported").ShouldEqual(GenerationDiagnosticCodes.UnsupportedArtifact);
    [Fact] void should_omit_the_unsupported_artifact_placement() => Disposition("handler:unsupported:placement").ShouldEqual(GenerationFactDisposition.OmittedWithDiagnostic);
    [Fact] void should_omit_the_unplaced_artifact_with_its_stable_diagnostic() => OmittedCode("event:unplaced").ShouldEqual(GenerationDiagnosticCodes.IncompleteArtifact);
    [Fact] void should_lower_the_consumed_relationship() => Disposition("relationship:produces").ShouldEqual(GenerationFactDisposition.Lowered);
    [Fact] void should_omit_the_unconsumed_relationship_with_its_stable_diagnostic() => OmittedCode("relationship:handles").ShouldEqual(GenerationDiagnosticCodes.UnsupportedRelationship);
    [Fact] void should_not_leave_any_fact_unknown() => _result.AdapterRun!.Facts.Any(record => record.Disposition == GenerationFactDisposition.Unknown).ShouldBeFalse();
    [Fact] void should_leave_input_dispositions_unknown() => _input.Facts.All(record => record.Disposition == GenerationFactDisposition.Unknown).ShouldBeTrue();
    [Fact] void should_return_a_new_snapshot() => ReferenceEquals(_result.AdapterRun, _input).ShouldBeFalse();
    [Fact] void should_not_change_the_result_after_with_copying_the_input() => _result.AdapterRun!.Facts.Length.ShouldEqual(11);
    [Fact] void should_allow_the_input_copy_to_diverge() => _copiedAfterGeneration.Facts.ShouldBeEmpty();

    GenerationFactDisposition Disposition(string id) => _result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == id).Disposition;

    GenerationFactDisposition[] Dispositions(params string[] ids) =>
        [.. ids.Select(Disposition)];

    string OmittedCode(string id) => _result.AdapterRun!.Facts
        .Single(record => record.Fact.Id.Value == id)
        .Diagnostics[0]
        .Code;

    Evidence Evidence(string path) => new()
    {
        Adapter = Adapter,
        Strength = EvidenceStrength.Exact,
        Source = new SourceRange
        {
            Path = path,
            StartLine = 1,
            StartColumn = 1,
            EndLine = 1,
            EndColumn = 1
        }
    };
}
