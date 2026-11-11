// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class a_coverage_fixture : a_specification_fixture
{
    protected static readonly SubjectId ConceptSubject = new() { Value = "dotnet://Banking/Concepts.AccountId" };

    protected GeneratedScreenplayDefinition Run(IEnumerable<GenerationFact> facts) =>
        Generator.Generate(Snapshot(Completed(Adapter, [.. facts])), new ScreenplayGenerationOptions { Domain = "Banking" });

    protected static string Records(GeneratedScreenplayDefinition result) => string.Join(
        '\n',
        result.AdapterRun.Facts
            .OrderBy(record => record.Fact.Id.Value, StringComparer.Ordinal)
            .Select(record => $"{record.Fact.Id.Value}|{record.Disposition}|{string.Join(" ;; ", record.Diagnostics.Select(diagnostic => $"{diagnostic.Code}|{diagnostic.Outcome}|{diagnostic.Message}"))}"));

    protected ConceptRepresentationFact Representation(string id, GenerationPrimitiveKind primitive) => new()
    {
        Id = new FactId { Value = id },
        Subject = ConceptSubject,
        Definition = new ConceptRepresentationDefinition
        {
            Concept = ConceptSubject,
            Kind = ConceptRepresentationKind.Primitive,
            Primitive = primitive
        },
        Evidence = EvidenceAt("Concepts/AccountId.cs", 1)
    };

    protected ConceptAttributeFact ConceptAttribute(string id, string reason) => new()
    {
        Id = new FactId { Value = id },
        Subject = ConceptSubject,
        Definition = new ConceptAttributeDefinition { Concept = ConceptSubject, Name = "pii", Reason = reason },
        Evidence = EvidenceAt("Concepts/AccountId.cs", 2)
    };

    protected ConceptValidationRuleFact ConceptRule(string id, string predicate) => new()
    {
        Id = new FactId { Value = id },
        Subject = ConceptSubject,
        Definition = new ConceptValidationRuleDefinition
        {
            Concept = ConceptSubject,
            RuleIdentity = "account-number-format",
            Kind = ConceptValidationRuleKind.NamedPredicate,
            Predicate = predicate
        },
        Evidence = EvidenceAt("Concepts/AccountId.cs", 3)
    };
}
