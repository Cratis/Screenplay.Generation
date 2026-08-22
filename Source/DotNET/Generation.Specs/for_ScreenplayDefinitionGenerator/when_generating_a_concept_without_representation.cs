// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_a_concept_without_representation : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Concepts.ExternalCode" };
        var evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact };
        var concept = new ArtifactFact
        {
            Id = new FactId { Value = "concept:ExternalCode" },
            Subject = subject,
            Definition = new ArtifactDefinition
            {
                Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept },
                Name = "ExternalCode",
                File = "Concepts/ExternalCode.cs"
            },
            Evidence = evidence
        };
        var placed = Event("CodeAssigned", "Assign", Property("code", "ExternalCode", subject));

        _result = Generator.Generate(
            [Contribution([concept, .. placed])],
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_report_the_missing_representation() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(GenerationDiagnosticCodes.MissingConceptRepresentation);
    [Fact] void should_report_the_unresolved_concept_reference() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(GenerationDiagnosticCodes.MissingConceptReference);
    [Fact] void should_not_emit_the_concept_as_string() => _result.Source.ShouldNotContain("concept ExternalCode : String");
    [Fact] void should_not_emit_an_artifact_with_the_unresolved_concept_reference() => _result.Source.ShouldNotContain("event CodeAssigned");
}
