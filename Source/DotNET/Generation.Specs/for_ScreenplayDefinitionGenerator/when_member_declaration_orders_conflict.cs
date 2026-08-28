// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_member_declaration_orders_conflict : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Ordering/Events.CustomerRegistered" };
        var artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Event };
        var evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact };
        var facts = new List<GenerationFact>
        {
            new ArtifactDeclarationFact
            {
                Id = new FactId { Value = "event:declaration" },
                Subject = subject,
                Evidence = evidence,
                Definition = new ArtifactDeclarationDefinition
                {
                    Artifact = artifact,
                    Name = "CustomerRegistered"
                }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "event:placement" },
                Subject = subject,
                Evidence = evidence,
                Artifact = artifact,
                Placement = new ArtifactPlacement
                {
                    Module = "Customers",
                    Slice = "Register",
                    SliceKind = GenerationSliceKind.StateChange
                }
            }
        };
        facts.AddRange(Member("first", 0, artifact, subject, evidence));
        facts.AddRange(Member("second", 0, artifact, subject, evidence));
        facts.AddRange(Member("unrelated", 1, artifact, subject, evidence));
        facts.AddRange(Member("fourth", 2, artifact, subject, evidence));
        facts.AddRange(Member("fifth", 2, artifact, subject, evidence));

        _result = Generator.Generate(
            Snapshot(Completed(Adapter, facts)),
            new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_report_the_duplicate_order_conflict() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingArtifactMember);
    [Fact] void should_omit_the_incomplete_granular_artifact() => _result.Graph.Artifacts.Any(artifact => artifact.Key.Kind == ArtifactKind.Event).ShouldBeFalse();
    [Fact] void should_conflict_members_occupying_the_first_duplicate_order() => Records("first", "second").All(record => record.Disposition == GenerationFactDisposition.Conflicted).ShouldBeTrue();
    [Fact] void should_conflict_members_occupying_the_later_duplicate_order() => Records("fourth", "fifth").All(record => record.Disposition == GenerationFactDisposition.Conflicted).ShouldBeTrue();
    [Fact] void should_not_conflict_the_unrelated_member() => Records("unrelated").Any(record => record.Disposition == GenerationFactDisposition.Conflicted).ShouldBeFalse();
    [Fact] void should_not_associate_the_duplicate_order_diagnostic_with_the_unrelated_member() => Records("unrelated").SelectMany(record => record.Diagnostics).Select(diagnostic => diagnostic.Code).ShouldNotContain(GenerationDiagnosticCodes.ConflictingArtifactMember);

    GenerationFactRecord[] Records(params string[] members) =>
    [
        .. _result.AdapterRun!.Facts.Where(record => members.Any(member => record.Fact.Id.Value.EndsWith(member, StringComparison.Ordinal)))
    ];

    static GenerationFact[] Member(
        string name,
        int order,
        ArtifactKey artifact,
        SubjectId subject,
        Evidence evidence)
    {
        var member = new ArtifactMemberKey { Artifact = artifact, Name = name };
        return
        [
            new ArtifactMemberDeclarationFact
            {
                Id = new FactId { Value = $"member:declaration:{name}" },
                Subject = subject,
                Evidence = evidence,
                Definition = new ArtifactMemberDeclarationDefinition
                {
                    Member = member,
                    DeclarationOrder = order
                }
            },
            new ArtifactMemberTypeUseFact
            {
                Id = new FactId { Value = $"member:type-use:{name}" },
                Subject = subject,
                Evidence = evidence,
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = member,
                    Type = new TypeUseDefinition { Name = "String" }
                }
            }
        ];
    }
}
