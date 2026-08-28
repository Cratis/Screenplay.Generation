// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_an_event_source_identifier_role : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var commandSubject = new SubjectId { Value = "dotnet://Ordering/Commands.SubmitOrder" };
        var eventSubject = new SubjectId { Value = "dotnet://Ordering/Events.OrderSubmitted" };
        var command = new ArtifactKey { Subject = commandSubject, Kind = ArtifactKind.Command };
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
    [Fact] void should_mark_the_exact_command_member_as_identifying() => Command().Definition.Properties.Single().IsIdentifier.ShouldBeTrue();
    [Fact] void should_lower_the_event_source_identifier_role() => _result.Source.ShouldContain("orderId Uuid identifier");
    [Fact] void should_classify_the_role_as_lowered() => _result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == "command:submit:event-source-identifier").Disposition.ShouldEqual(GenerationFactDisposition.Lowered);

    ResolvedArtifactVariant Command() => _result.Graph.Artifacts.Single(artifact => artifact.Key.Kind == ArtifactKind.Command).Variants.Single();
}
