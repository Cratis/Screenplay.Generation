// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_invalid_enumeration_values : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Concepts.Status" };
        var evidence = new Evidence
        {
            Adapter = Adapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange
            {
                Path = "Concepts/Status.cs",
                StartLine = 3,
                StartColumn = 1,
                EndLine = 3,
                EndColumn = 30
            }
        };
        _result = Generator.Generate(
        [
            Contribution(
                new ArtifactFact
                {
                    Id = new FactId { Value = "concept" }, Subject = subject, Evidence = evidence,
                    Definition = new ArtifactDefinition { Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept }, Name = "Status" }
                },
                new ConceptRepresentationFact
                {
                    Id = new FactId { Value = "representation" }, Subject = subject, Evidence = evidence,
                    Definition = new ConceptRepresentationDefinition
                    {
                        Concept = subject,
                        Kind = ConceptRepresentationKind.Enumeration,
                        EnumerationValues = ["Ready", "ready"]
                    }
                })
        ],
        new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_report_the_unsupported_representation() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(GenerationDiagnosticCodes.UnsupportedConceptRepresentation);
    [Fact] void should_preserve_the_representation_source() => _result.Diagnostics.Single().Source!.Path.ShouldEqual("Concepts/Status.cs");
    [Fact] void should_not_emit_the_invalid_enumeration() => _result.Source.ShouldNotContain("concept Status");
}
