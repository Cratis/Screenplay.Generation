// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_calculating_relationship_conflict_dispositions : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var commandSubject = new SubjectId { Value = "dotnet://Banking/Commands.OpenAccount" };
        var eventFacts = Event("AccountOpened", "Open");
        var eventArtifact = (ArtifactFact)eventFacts[0];
        var commandKey = new ArtifactKey { Subject = commandSubject, Kind = ArtifactKind.Command };
        var placement = ((ArtifactPlacementFact)eventFacts[1]).Placement;
        var first = Relationship("relationship:first", commandSubject, eventArtifact.Subject, "result");
        var second = Relationship("relationship:second", commandSubject, eventArtifact.Subject, "events");
        _result = Generator.Generate(
            Snapshot(Completed(
                Adapter,
                [
                    .. eventFacts,
                    new ArtifactFact
                    {
                        Id = new FactId { Value = "command" },
                        Subject = commandSubject,
                        Evidence = Exact(),
                        Definition = new ArtifactDefinition { Key = commandKey, Name = "OpenAccount" }
                    },
                    new ArtifactPlacementFact
                    {
                        Id = new FactId { Value = "command:placement" },
                        Subject = commandSubject,
                        Evidence = Exact(),
                        Artifact = commandKey,
                        Placement = placement
                    },
                    first,
                    second
                ])),
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_classify_the_first_relationship_variant_as_conflicted() => Disposition("relationship:first").ShouldEqual(GenerationFactDisposition.Conflicted);
    [Fact] void should_classify_the_second_relationship_variant_as_conflicted() => Disposition("relationship:second").ShouldEqual(GenerationFactDisposition.Conflicted);
    [Fact] void should_associate_the_relationship_conflict_diagnostic_with_both_variants() => _result.AdapterRun!.Facts.Where(record => record.Fact is RelationshipFact).All(record => record.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingRelationship)).ShouldBeTrue();
    [Fact] void should_not_leave_any_fact_unknown() => _result.AdapterRun!.Facts.Any(record => record.Disposition == GenerationFactDisposition.Unknown).ShouldBeFalse();

    RelationshipFact Relationship(string id, SubjectId source, SubjectId target, string sourceMember) => new()
    {
        Id = new FactId { Value = id },
        Subject = source,
        Evidence = Exact(),
        Definition = new RelationshipDefinition
        {
            Key = new RelationshipKey
            {
                Kind = RelationshipKind.Produces,
                Source = source,
                Target = target
            },
            SourceMember = sourceMember
        }
    };

    Evidence Exact() => new() { Adapter = Adapter, Strength = EvidenceStrength.Exact };

    GenerationFactDisposition Disposition(string id) => _result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == id).Disposition;
}
