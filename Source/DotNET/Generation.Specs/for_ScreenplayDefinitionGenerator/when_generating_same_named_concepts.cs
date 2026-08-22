// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_same_named_concepts : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var first = new SubjectId { Value = "dotnet://First/Concepts.CustomerId" };
        var second = new SubjectId { Value = "dotnet://Second/Concepts.CustomerId" };
        var evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact };
        _result = Generator.Generate(
        [
            Contribution(
                Artifact("first-artifact", first, evidence),
                Representation("first-representation", first, evidence),
                Artifact("second-artifact", second, evidence),
                Representation("second-representation", second, evidence))
        ],
        new ScreenplayGenerationOptions { Domain = "Customers" });
    }

    [Fact] void should_report_the_name_conflict() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingConceptName);
    [Fact] void should_not_emit_an_ambiguous_concept() => _result.Source.ShouldNotContain("concept CustomerId");

    static ArtifactFact Artifact(string id, SubjectId subject, Evidence evidence) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Definition = new ArtifactDefinition
        {
            Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept },
            Name = "CustomerId"
        },
        Evidence = evidence
    };

    static ConceptRepresentationFact Representation(string id, SubjectId subject, Evidence evidence) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Definition = new ConceptRepresentationDefinition
        {
            Concept = subject,
            Kind = ConceptRepresentationKind.Primitive,
            Primitive = GenerationPrimitiveKind.Uuid
        },
        Evidence = evidence
    };
}
