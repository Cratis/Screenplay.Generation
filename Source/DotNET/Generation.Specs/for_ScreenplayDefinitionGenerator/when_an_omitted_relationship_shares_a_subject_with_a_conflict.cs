// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_omitted_relationship_shares_a_subject_with_a_conflict : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var source = new SubjectId { Value = "dotnet://Banking/Handlers.AccountHandler" };
        var firstTarget = new SubjectId { Value = "dotnet://Banking/Commands.OpenAccount" };
        var secondTarget = new SubjectId { Value = "dotnet://Banking/ReadModels.Account" };
        _result = Generator.Generate(
            Snapshot(Completed(
                Adapter,
                [
                    Relationship("handles", RelationshipKind.Handles, source, firstTarget, null),
                    Relationship("reads:first", RelationshipKind.Reads, source, secondTarget, "first"),
                    Relationship("reads:second", RelationshipKind.Reads, source, secondTarget, "second")
                ])),
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_omit_the_unconsumed_handles_relationship() => Record("handles").Disposition.ShouldEqual(GenerationFactDisposition.OmittedWithDiagnostic);
    [Fact] void should_report_the_stable_unsupported_relationship_diagnostic() => Record("handles").Diagnostics.Select(_ => _.Code).ShouldContain(GenerationDiagnosticCodes.UnsupportedRelationship);
    [Fact] void should_not_borrow_the_reads_conflict_diagnostic() => Record("handles").Diagnostics.Select(_ => _.Code).ShouldNotContain(GenerationDiagnosticCodes.ConflictingRelationship);
    [Fact] void should_classify_both_reads_variants_as_conflicted() => new[] { Record("reads:first"), Record("reads:second") }.All(_ => _.Disposition == GenerationFactDisposition.Conflicted).ShouldBeTrue();

    RelationshipFact Relationship(
        string id,
        RelationshipKind kind,
        SubjectId source,
        SubjectId target,
        string? sourceMember) => new()
    {
        Id = new FactId { Value = id },
        Subject = source,
        Evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact },
        Definition = new RelationshipDefinition
        {
            Key = new RelationshipKey { Kind = kind, Source = source, Target = target },
            SourceMember = sourceMember
        }
    };

    GenerationFactRecord Record(string id) =>
        _result.AdapterRun!.Facts.Single(_ => _.Fact.Id.Value == id);
}
