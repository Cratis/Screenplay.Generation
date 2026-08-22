// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_an_invalid_concept_representation : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Concepts.ExternalCode" };
        var evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact };
        _result = Generator.Generate(
        [
            Contribution(
                new ArtifactFact
                {
                    Id = new FactId { Value = "concept:ExternalCode" },
                    Subject = subject,
                    Definition = new ArtifactDefinition
                    {
                        Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept },
                        Name = "ExternalCode"
                    },
                    Evidence = evidence
                },
                new ConceptRepresentationFact
                {
                    Id = new FactId { Value = "representation:ExternalCode" },
                    Subject = subject,
                    Definition = new ConceptRepresentationDefinition
                    {
                        Concept = subject,
                        Kind = ConceptRepresentationKind.Primitive
                    },
                    Evidence = evidence
                })
        ],
        new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_report_the_unsupported_representation() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(GenerationDiagnosticCodes.UnsupportedConceptRepresentation);
    [Fact] void should_not_fall_back_to_string() => _result.Source.ShouldNotContain("concept ExternalCode : String");
}
