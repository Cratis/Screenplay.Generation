// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_concepts_with_observed_evidence : given.a_lowering_fixture
{
    readonly List<string> _reads = [];
    ScreenplayLoweringResult _result = null!;

    void Because()
    {
        var good = Concept("Good", GenerationPrimitiveKind.Text);
        var bad = Concept("Bad", GenerationPrimitiveKind.Text);
        var worse = Concept("Worse", GenerationPrimitiveKind.Text);
        GenerationFact[] facts =
        [
            .. good.Facts,
            .. bad.Facts,
            .. worse.Facts.Select(_ => _ is ConceptRepresentationFact fact
                ? fact with { Definition = fact.Definition with { Kind = ConceptRepresentationKind.Enumeration, Primitive = null, EnumerationValues = ["Open", "File"] } }
                : _),
            Attribute("good-attribute", good.Subject, "pii"),
            Attribute("bad-attribute", bad.Subject, "Bad Name"),
            Validation("good-rule", good.Subject, "good-length", "HaveLength"),
            Validation("bad-rule", bad.Subject, "bad-length", "1bad")
        ];
        var graph = new GenerationResolver().Resolve([Contribution(facts)]);
        var observed = graph with
        {
            ConceptRepresentations = [.. graph.ConceptRepresentations.Select(_ => _ with { Variants = [.. _.Variants.Select(variant => variant with { Evidence = Observe($"representation:{NameOf(_.Concept)}", variant.Evidence) })] })],
            ConceptAttributes = [.. graph.ConceptAttributes.Select(_ => _ with { Variants = [.. _.Variants.Select(variant => variant with { Evidence = Observe($"attribute:{NameOf(_.Concept)}", variant.Evidence) })] })],
            ConceptValidationRules = [.. graph.ConceptValidationRules.Select(_ => _ with { Variants = [.. _.Variants.Select(variant => variant with { Evidence = Observe($"rule:{NameOf(_.Concept)}", variant.Evidence) })] })]
        };

        _result = new ScreenplayLowerer().Lower(observed, "Banking");
    }

    [Fact]
    void should_only_read_the_evidence_of_rejected_definitions_once_each() => string.Join('\n', _reads).ShouldEqual(
        """
        attribute:Bad:enumerate
        rule:Bad:enumerate
        representation:Worse:enumerate
        """);

    [Fact]
    void should_report_the_complete_fixed_diagnostics() => Describe(_result.Diagnostics).ShouldEqual(
        """
        GEN0013 | Warning | Unsupported | dotnet://Banking/Concepts.Worse | Concepts/Worse.cs:1:1-1:1 | Concept 'Worse' has empty or duplicate enumeration values after Screenplay naming and was omitted
        GEN0019 | Warning | Unsupported | dotnet://Banking/Concepts.Bad | Concepts/Attributes.cs:4:1-4:20 | Concept 'Bad' has invalid attribute name 'Bad Name', which was omitted
        GEN0021 | Warning | Unsupported | dotnet://Banking/Concepts.Bad | - | Concept 'Bad' validation rule 'bad-length' has invalid or missing required data and was omitted
        """);

    static string NameOf(SubjectId subject) => subject.Value.Split('.')[^1];

    ConceptAttributeFact Attribute(string id, SubjectId subject, string name) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Definition = new ConceptAttributeDefinition { Concept = subject, Name = name },
        Evidence = EvidenceAt("Concepts/Attributes.cs", 4)
    };

    IReadOnlyList<Evidence> Observe(string label, IReadOnlyList<Evidence> evidence) =>
        new given.an_observed_list<Evidence>(evidence, read => _reads.Add($"{label}:{read}"));
}
