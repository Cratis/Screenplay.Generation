// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_source_members_normalize_to_the_same_name : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Ordering/Types.CaseCollision" };
        var artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.CompositeType };
        var member = new ArtifactMemberKey { Artifact = artifact, Name = "uRL" };
        var evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact };
        var facts = new GenerationFact[]
        {
            new ArtifactDeclarationFact
            {
                Id = new FactId { Value = "type:declaration" },
                Subject = subject,
                Evidence = evidence,
                Definition = new ArtifactDeclarationDefinition
                {
                    Artifact = artifact,
                    Name = "CaseCollision"
                }
            },
            Member("member:URL", 0, subject, member, evidence),
            Member("member:uRL", 1, subject, member, evidence)
        };

        _result = Generator.Generate(
            Snapshot(Completed(Adapter, facts)),
            new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_report_the_normalized_member_conflict() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingArtifactMember);
    [Fact] void should_omit_the_conflicted_granular_artifact() => _result.Graph.Artifacts.Any(artifact => artifact.Key.Kind == ArtifactKind.CompositeType).ShouldBeFalse();
    [Fact] void should_conflict_both_exact_source_member_assertions() => _result.AdapterRun!.Facts.Where(record => record.Fact is ArtifactMemberDeclarationFact).All(record => record.Disposition == GenerationFactDisposition.Conflicted).ShouldBeTrue();

    static ArtifactMemberDeclarationFact Member(
        string id,
        int order,
        SubjectId subject,
        ArtifactMemberKey member,
        Evidence evidence) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Evidence = evidence,
        Definition = new ArtifactMemberDeclarationDefinition
        {
            Member = member,
            DeclarationOrder = order
        }
    };
}
