// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_calculating_collision_safe_fact_dispositions : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var artifactSubject = new SubjectId { Value = "dotnet://Banking/Events.AccountOpened" };
        var artifactKey = new ArtifactKey { Subject = artifactSubject, Kind = ArtifactKind.Event };
        var source = new SubjectId { Value = "dotnet://Banking/Handlers.AccountHandler" };
        var target = new SubjectId { Value = "dotnet://Banking/ReadModels.Account" };
        _result = Generator.Generate(
            Snapshot(Completed(
                Adapter,
                [
                    Artifact("artifact:null", artifactKey, null),
                    Artifact("artifact:empty", artifactKey, string.Empty),
                    Relationship("relationship:null", source, target, null, null),
                    Relationship("relationship:empty-source", source, target, string.Empty, null),
                    Relationship(
                        "relationship:separator-target",
                        source,
                        new SubjectId { Value = $"{target.Value}\u001fmember" },
                        null,
                        null),
                    Relationship("relationship:separator-discriminator", source, target, null, "member")
                ])),
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_conflict_null_and_empty_artifact_files_independently() => Dispositions("artifact:null", "artifact:empty").ShouldContainOnly(GenerationFactDisposition.Conflicted, GenerationFactDisposition.Conflicted);
    [Fact] void should_conflict_null_and_empty_relationship_members_independently() => Dispositions("relationship:null", "relationship:empty-source").ShouldContainOnly(GenerationFactDisposition.Conflicted, GenerationFactDisposition.Conflicted);
    [Fact] void should_omit_separator_bearing_relationship_keys_independently() => Dispositions("relationship:separator-target", "relationship:separator-discriminator").ShouldContainOnly(GenerationFactDisposition.OmittedWithDiagnostic, GenerationFactDisposition.OmittedWithDiagnostic);
    [Fact] void should_not_report_a_conflict_for_separator_bearing_relationship_keys() => Records("relationship:separator-target", "relationship:separator-discriminator").SelectMany(_ => _.Diagnostics).Any(_ => _.Outcome == GenerationDiagnosticOutcome.Conflict).ShouldBeFalse();

    ArtifactFact Artifact(string id, ArtifactKey key, string? file) => new()
    {
        Id = new FactId { Value = id },
        Subject = key.Subject,
        Evidence = Exact(),
        Definition = new ArtifactDefinition { Key = key, Name = "AccountOpened", File = file }
    };

    RelationshipFact Relationship(
        string id,
        SubjectId source,
        SubjectId target,
        string? sourceMember,
        string? discriminator) => new()
    {
        Id = new FactId { Value = id },
        Subject = source,
        Evidence = Exact(),
        Definition = new RelationshipDefinition
        {
            Key = new RelationshipKey
            {
                Kind = RelationshipKind.Reads,
                Source = source,
                Target = target,
                Discriminator = discriminator
            },
            SourceMember = sourceMember
        }
    };

    Evidence Exact() => new() { Adapter = Adapter, Strength = EvidenceStrength.Exact };

    GenerationFactRecord[] Records(params string[] ids) =>
        [.. ids.Select(id => _result.AdapterRun!.Facts.Single(_ => _.Fact.Id.Value == id))];

    GenerationFactDisposition[] Dispositions(params string[] ids) =>
        [.. Records(ids).Select(_ => _.Disposition)];
}
