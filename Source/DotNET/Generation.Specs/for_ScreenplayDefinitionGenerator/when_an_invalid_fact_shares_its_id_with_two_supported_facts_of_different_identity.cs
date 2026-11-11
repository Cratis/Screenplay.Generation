// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_invalid_fact_shares_its_id_with_two_supported_facts_of_different_identity : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because() => _result = Generator.Generate(
        Snapshot(Completed(Adapter, [Artifact(EvidenceStrength.Unknown, "Invalid"), Artifact(EvidenceStrength.Exact, "First"), Artifact(EvidenceStrength.Exact, "Second")])),
        new ScreenplayGenerationOptions { Domain = "Banking" });

    [Fact] void should_record_every_fact_in_canonical_order() => _result.AdapterRun.Facts.Select(_ => ((ArtifactFact)_.Fact).Definition.Name).ShouldEqual(["First", "Second", "Invalid"]);
    [Fact] void should_conflict_the_supported_ones_and_omit_the_invalid_fact() => _result.AdapterRun.Facts.Select(_ => _.Disposition).ShouldEqual([GenerationFactDisposition.Conflicted, GenerationFactDisposition.Conflicted, GenerationFactDisposition.OmittedWithDiagnostic]);

    ArtifactFact Artifact(EvidenceStrength strength, string name)
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Events.AccountOpened" };
        return new()
        {
            Id = new FactId { Value = "artifact:shared" },
            Subject = subject,
            Evidence = new Evidence { Adapter = Adapter, Strength = strength },
            Definition = new ArtifactDefinition
            {
                Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Event },
                Name = name
            }
        };
    }
}
