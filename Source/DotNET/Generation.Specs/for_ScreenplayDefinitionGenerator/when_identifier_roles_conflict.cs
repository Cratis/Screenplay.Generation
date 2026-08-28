// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_identifier_roles_conflict : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var commandSubject = new SubjectId { Value = "dotnet://Ordering/Commands.SubmitOrder" };
        var eventSubject = new SubjectId { Value = "dotnet://Ordering/Events.OrderSubmitted" };
        var unrelatedSubject = new SubjectId { Value = "dotnet://Ordering/Events.AuditRecorded" };
        var command = new ArtifactKey { Subject = commandSubject, Kind = ArtifactKind.Command };
        var @event = new ArtifactKey { Subject = eventSubject, Kind = ArtifactKind.Event };
        var member = new ArtifactMemberKey { Artifact = command, Name = "orderId" };
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
            Role("role:identifier", ArtifactMemberRoleKind.Identifier, commandSubject, member, evidence),
            Role("role:event-source-identifier", ArtifactMemberRoleKind.EventSourceIdentifier, commandSubject, member, evidence),
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "command:placement" },
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
                Id = new FactId { Value = "event:placement" },
                Subject = eventSubject,
                Evidence = evidence,
                Artifact = @event,
                Placement = placement
            },
            new ArtifactFact
            {
                Id = new FactId { Value = "orderId" },
                Subject = unrelatedSubject,
                Evidence = evidence,
                Definition = new ArtifactDefinition
                {
                    Key = new ArtifactKey { Subject = unrelatedSubject, Kind = ArtifactKind.Event },
                    Name = "AuditRecorded"
                }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "audit:placement" },
                Subject = unrelatedSubject,
                Evidence = evidence,
                Artifact = new ArtifactKey { Subject = unrelatedSubject, Kind = ArtifactKind.Event },
                Placement = placement
            },
            new RelationshipFact
            {
                Id = new FactId { Value = "command:produces" },
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

    [Fact] void should_fail_closed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_role_conflict() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingArtifactMember);
    [Fact] void should_omit_the_command_instead_of_choosing_either_role() => _result.Graph.Artifacts.Any(artifact => artifact.Key.Kind == ArtifactKind.Command).ShouldBeFalse();
    [Fact] void should_conflict_both_role_facts() => Dispositions().ShouldContainOnly(GenerationFactDisposition.Conflicted, GenerationFactDisposition.Conflicted);
    [Fact] void should_not_emit_identifier_semantics() => _result.Source.ShouldNotContain("orderId Uuid identifier");
    [Fact] void should_not_associate_a_quoted_member_name_with_an_unrelated_legacy_fact_id() => _result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == "orderId").Disposition.ShouldEqual(GenerationFactDisposition.Lowered);

    GenerationFactDisposition[] Dispositions() =>
    [
        .. _result.AdapterRun!.Facts
            .Where(record => record.Fact is ArtifactMemberRoleFact)
            .Select(record => record.Disposition)
    ];

    static ArtifactMemberRoleFact Role(
        string id,
        ArtifactMemberRoleKind role,
        SubjectId subject,
        ArtifactMemberKey member,
        Evidence evidence) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Evidence = evidence,
        Definition = new ArtifactMemberRoleDefinition { Member = member, Role = role }
    };
}
