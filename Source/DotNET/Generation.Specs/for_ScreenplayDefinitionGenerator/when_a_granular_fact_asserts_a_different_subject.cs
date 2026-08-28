// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_granular_fact_asserts_a_different_subject : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var artifactSubject = new SubjectId { Value = "dotnet://Ordering/Commands.SubmitOrder" };
        var foreignSubject = new SubjectId { Value = "dotnet://Foreign/Commands.SubmitOrder" };
        var artifact = new ArtifactKey { Subject = artifactSubject, Kind = ArtifactKind.Command };
        var eventSubject = new SubjectId { Value = "dotnet://Ordering/Events.OrderSubmitted" };
        var hiddenSubject = new SubjectId { Value = "dotnet://Ordering/Events.HiddenEvent" };
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
                Subject = artifactSubject,
                Evidence = evidence,
                Definition = new ArtifactDefinition
                {
                    Key = artifact,
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
            new ArtifactDeclarationFact
            {
                Id = new FactId { Value = "foreign:declaration" },
                Subject = foreignSubject,
                Evidence = evidence,
                Definition = new ArtifactDeclarationDefinition
                {
                    Artifact = new ArtifactKey { Subject = hiddenSubject, Kind = ArtifactKind.Event },
                    Name = "HiddenEvent"
                }
            },
            new ArtifactMemberRoleFact
            {
                Id = new FactId { Value = "foreign:role" },
                Subject = foreignSubject,
                Evidence = evidence,
                Definition = new ArtifactMemberRoleDefinition
                {
                    Member = new ArtifactMemberKey { Artifact = artifact, Name = "orderId" },
                    Role = ArtifactMemberRoleKind.EventSourceIdentifier
                }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "command:placement" },
                Subject = artifactSubject,
                Evidence = evidence,
                Artifact = artifact,
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
                Id = new FactId { Value = "event:placement" },
                Subject = eventSubject,
                Evidence = evidence,
                Artifact = @event,
                Placement = placement
            },
            new RelationshipFact
            {
                Id = new FactId { Value = "command:produces" },
                Subject = artifactSubject,
                Evidence = evidence,
                Definition = new RelationshipDefinition
                {
                    Key = new RelationshipKey
                    {
                        Kind = RelationshipKind.Produces,
                        Source = artifactSubject,
                        Target = eventSubject
                    }
                }
            }
        };

        _result = Generator.Generate(
            [new AdapterContribution { Adapter = Adapter, Facts = facts }],
            new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_report_invalid_granular_ownership() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.InvalidGranularFactOwnership);
    [Fact] void should_not_create_an_artifact_from_the_foreign_declaration() => _result.Graph.Artifacts.Any(artifact => artifact.Key.Subject.Value == "dotnet://Ordering/Events.HiddenEvent").ShouldBeFalse();
    [Fact] void should_not_apply_the_foreign_role() => Command().Definition.Properties.Single().IsIdentifier.ShouldBeFalse();
    [Fact] void should_not_emit_identifier_semantics() => _result.Source.ShouldNotContain("orderId Uuid identifier");

    ResolvedArtifactVariant Command() => _result.Graph.Artifacts.Single(artifact => artifact.Key.Kind == ArtifactKind.Command).Variants.Single();
}
