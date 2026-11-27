// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_facts_sharing_an_id_carry_linked_lowering_diagnostics : given.a_generator
{
    GeneratedScreenplayDefinition[] _results = [];

    void Because()
    {
        var first = new SubjectId { Value = "dotnet://First/Concepts.CustomerId" };
        var second = new SubjectId { Value = "dotnet://Second/Concepts.CustomerId" };
        var third = new SubjectId { Value = "dotnet://Third/Concepts.Other" };
        var evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact };
        GenerationFact[] facts =
        [
            Artifact("first-artifact", first, evidence),
            Representation("first-representation", first, evidence),
            Artifact("second-artifact", second, evidence),
            Representation("second-representation", second, evidence),
            Representation("first-artifact", third, evidence)
        ];
        var options = new ScreenplayGenerationOptions { Domain = "Customers" };
        _results = [Generator.Generate(Snapshot(Completed(Adapter, facts)), options), Generator.Generate(Snapshot(Completed(Adapter, [.. facts.Reverse()])), options)];
    }

    [Fact] void should_publish_the_name_conflict_once_in_the_run() => _results.All(result => Count(result.AdapterRun.Diagnostics) == 1).ShouldBeTrue();
    [Fact] void should_publish_the_name_conflict_once_outward() => _results.All(result => Count(result.Diagnostics) == 1).ShouldBeTrue();

    static int Count(IEnumerable<GenerationDiagnostic> diagnostics) => diagnostics.Count(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingConceptName);

    static ArtifactFact Artifact(string id, SubjectId subject, Evidence evidence) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Definition = new ArtifactDefinition { Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept }, Name = "CustomerId" },
        Evidence = evidence
    };

    static ConceptRepresentationFact Representation(string id, SubjectId subject, Evidence evidence) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Definition = new ConceptRepresentationDefinition { Concept = subject, Kind = ConceptRepresentationKind.Primitive, Primitive = GenerationPrimitiveKind.Uuid },
        Evidence = evidence
    };
}
