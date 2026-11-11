// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_invalid_fact_is_explicitly_linked_to_a_conflict_diagnostic : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Events.AccountOpened" };
        var fact = new ArtifactFact
        {
            Id = new FactId { Value = "artifact:invalid-strength" },
            Subject = subject,
            Evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Unknown },
            Definition = new ArtifactDefinition
            {
                Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Event },
                Name = "AccountOpened"
            }
        };
        var conflict = new GenerationDiagnostic
        {
            Code = GenerationDiagnosticCodes.ConflictingArtifact,
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = GenerationDiagnosticOutcome.Conflict,
            Message = "Explicit conflict",
            Facts = [fact.Id],
            Subject = subject
        };
        _result = Generator.Generate(
            Snapshot(Completed(Adapter, [fact], [conflict])),
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_keep_the_omission_instead_of_conflicting() => Record().Disposition.ShouldEqual(GenerationFactDisposition.OmittedWithDiagnostic);

    GenerationFactRecord Record() => _result.AdapterRun.Facts.Single(_ => _.Fact.Id.Value == "artifact:invalid-strength");
}
