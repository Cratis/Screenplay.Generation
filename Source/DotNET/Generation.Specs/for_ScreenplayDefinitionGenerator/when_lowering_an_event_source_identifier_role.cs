// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_an_event_source_identifier_role : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var commandSubject = new SubjectId { Value = "dotnet://Ordering/Commands.SubmitOrder" };
        var trackingSubject = new SubjectId { Value = "dotnet://Ordering/Commands.TrackOrder" };
        var eventSubject = new SubjectId { Value = "dotnet://Ordering/Events.OrderSubmitted" };
        var command = new ArtifactKey { Subject = commandSubject, Kind = ArtifactKind.Command };
        var trackingCommand = new ArtifactKey { Subject = trackingSubject, Kind = ArtifactKind.Command };
        var @event = new ArtifactKey { Subject = eventSubject, Kind = ArtifactKind.Event };
        var evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact };
        var placement = new ArtifactPlacement
        {
            Module = "Orders",
            Slice = "Submit",
            SliceKind = GenerationSliceKind.StateChange
        };
        var facts = new GenerationFact[]
        {
            new ArtifactFact
            {
                Id = new FactId { Value = "command:submit" },
                Subject = commandSubject,
                Evidence = evidence,
                Definition = new ArtifactDefinition
                {
                    Key = command,
                    Name = "SubmitOrder",
                    Properties =
                    [
                        new PropertyDefinition
                        {
                            Name = "orderId",
                            Type = new TypeReferenceDefinition { Name = "Uuid" }
                        }
                    ]
                }
            },
            new ArtifactMemberRoleFact
            {
                Id = new FactId { Value = "command:submit:event-source-identifier" },
                Subject = commandSubject,
                Evidence = evidence,
                Definition = new ArtifactMemberRoleDefinition
                {
                    Member = new ArtifactMemberKey { Artifact = command, Name = "orderId" },
                    Role = ArtifactMemberRoleKind.EventSourceIdentifier
                }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "command:submit:placement" },
                Subject = commandSubject,
                Evidence = evidence,
                Artifact = command,
                Placement = placement
            },
            new ArtifactFact
            {
                Id = new FactId { Value = "command:track" },
                Subject = trackingSubject,
                Evidence = evidence,
                Definition = new ArtifactDefinition
                {
                    Key = trackingCommand,
                    Name = "TrackOrder",
                    Properties =
                    [
                        new PropertyDefinition
                        {
                            Name = "trackingId",
                            Type = new TypeReferenceDefinition { Name = "Uuid" }
                        }
                    ]
                }
            },
            new ArtifactMemberRoleFact
            {
                Id = new FactId { Value = "command:track:identifier" },
                Subject = trackingSubject,
                Evidence = evidence,
                Definition = new ArtifactMemberRoleDefinition
                {
                    Member = new ArtifactMemberKey { Artifact = trackingCommand, Name = "trackingId" },
                    Role = ArtifactMemberRoleKind.Identifier
                }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "command:track:placement" },
                Subject = trackingSubject,
                Evidence = evidence,
                Artifact = trackingCommand,
                Placement = placement
            },
            new ArtifactFact
            {
                Id = new FactId { Value = "event:submitted" },
                Subject = eventSubject,
                Evidence = evidence,
                Definition = new ArtifactDefinition { Key = @event, Name = "OrderSubmitted" }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "event:submitted:placement" },
                Subject = eventSubject,
                Evidence = evidence,
                Artifact = @event,
                Placement = placement
            },
            new RelationshipFact
            {
                Id = new FactId { Value = "command:track:produces" },
                Subject = trackingSubject,
                Evidence = evidence,
                Definition = new RelationshipDefinition
                {
                    Key = new RelationshipKey
                    {
                        Kind = RelationshipKind.Produces,
                        Source = trackingSubject,
                        Target = eventSubject
                    }
                }
            },
            new RelationshipFact
            {
                Id = new FactId { Value = "command:submit:produces" },
                Subject = commandSubject,
                Evidence = evidence,
                Definition = new RelationshipDefinition
                {
                    Key = new RelationshipKey
                    {
                        Kind = RelationshipKind.Produces,
                        Source = commandSubject,
                        Target = eventSubject
                    }
                }
            }
        };

        _result = Generator.Generate(
            Snapshot(Completed(Adapter, facts)),
            new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_generate_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_mark_the_exact_event_source_member_as_identifying() => Command("SubmitOrder").Definition.Properties.Single().IsIdentifier.ShouldBeTrue();
    [Fact] void should_not_collapse_an_ordinary_identifier_into_event_source_semantics() => Command("TrackOrder").Definition.Properties.Single().IsIdentifier.ShouldBeFalse();
    [Fact] void should_lower_the_event_source_identifier_role() => _result.Source.ShouldContain("orderId Uuid identifier");
    [Fact] void should_classify_the_event_source_role_as_lowered() => _result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == "command:submit:event-source-identifier").Disposition.ShouldEqual(GenerationFactDisposition.Lowered);
    [Fact] void should_retain_the_ordinary_identifier_role_as_provenance() => _result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == "command:track:identifier").Disposition.ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_not_emit_ordinary_identifier_semantics() => _result.Source.ShouldNotContain("trackingId Uuid identifier");

    ResolvedArtifactVariant Command(string name) => _result.Graph.Artifacts
        .Where(artifact => artifact.Key.Kind == ArtifactKind.Command)
        .SelectMany(artifact => artifact.Variants)
        .Single(variant => variant.Definition.Name == name);
}
