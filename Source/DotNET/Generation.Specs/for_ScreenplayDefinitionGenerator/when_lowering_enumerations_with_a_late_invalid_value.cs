// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_enumerations_with_a_late_invalid_value : given.a_lowering_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because() =>
        _result = Generator.Generate(
            [Contribution([
                .. Enumeration("Fine", 1, "Open", "Closed"),
                .. Enumeration("Keyword", 2, "Open", "File"),
                .. Enumeration("Duplicate", 3, "Open", "Closed", "closed"),
                .. Enumeration("Empty", 4, "Open", string.Empty),
                .. Enumeration("Digit", 5, "Open", "1Bad")])],
            new ScreenplayGenerationOptions { Domain = "Banking" });

    [Fact]
    void should_emit_only_the_enumeration_whose_values_are_all_valid() => _result.Source.ReplaceLineEndings("\n").ShouldEqual(
        """
        domain Banking

        concept Fine : Enum
          file Concepts/Fine.cs
          open
          closed

        """.ReplaceLineEndings("\n"));

    [Fact]
    void should_report_the_complete_fixed_diagnostics() => Describe(_result.Diagnostics).ShouldEqual(
        """
        GEN0013 | Warning | Unsupported | dotnet://Banking/Concepts.Digit | Concepts/Digit.cs:5:1-5:20 | Concept 'Digit' has empty or duplicate enumeration values after Screenplay naming and was omitted
        GEN0013 | Warning | Unsupported | dotnet://Banking/Concepts.Empty | Concepts/Empty.cs:4:1-4:20 | Concept 'Empty' has empty or duplicate enumeration values after Screenplay naming and was omitted
        GEN0013 | Warning | Unsupported | dotnet://Banking/Concepts.Keyword | Concepts/Keyword.cs:2:1-2:20 | Concept 'Keyword' has empty or duplicate enumeration values after Screenplay naming and was omitted
        GEN0013 | Warning | Unsupported | dotnet://Banking/Concepts.Duplicate | Concepts/Duplicate.cs:3:1-3:20 | Concept 'Duplicate' has empty or duplicate enumeration values after Screenplay naming and was omitted
        """);

    GenerationFact[] Enumeration(string name, int line, params string[] values)
    {
        var subject = SubjectOf($"Concepts.{name}");
        var evidence = EvidenceAt($"Concepts/{name}.cs", line);
        return
        [
            new ArtifactFact
            {
                Id = new FactId { Value = $"concept:{name}" },
                Subject = subject,
                Definition = new ArtifactDefinition
                {
                    Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept },
                    Name = name,
                    File = $"Concepts/{name}.cs"
                },
                Evidence = evidence
            },
            new ConceptRepresentationFact
            {
                Id = new FactId { Value = $"concept-representation:{name}" },
                Subject = subject,
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = subject,
                    Kind = ConceptRepresentationKind.Enumeration,
                    EnumerationValues = values
                },
                Evidence = evidence
            }
        ];
    }
}
