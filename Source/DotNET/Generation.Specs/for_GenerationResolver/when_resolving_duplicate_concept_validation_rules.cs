// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_duplicate_concept_validation_rules : given.facts
{
    ResolvedApplicationGraph _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Concepts.AccountNumber" };
        _result = new GenerationResolver().Resolve(
        [
            Contribution(FirstAdapter, Rule("first", FirstAdapter, subject)),
            Contribution(SecondAdapter, Rule("second", SecondAdapter, subject))
        ]);
    }

    [Fact] void should_resolve_one_rule_definition() => _result.ConceptValidationRules.Single().Variants.Count.ShouldEqual(1);
    [Fact] void should_merge_both_evidence_sources() => _result.ConceptValidationRules.Single().Variants.Single().Evidence.Count.ShouldEqual(2);
    [Fact] void should_keep_the_rule_identity() => _result.ConceptValidationRules.Single().RuleIdentity.ShouldEqual("account-number-format");
    [Fact] void should_not_report_a_conflict() => _result.Diagnostics.ShouldBeEmpty();

    static ConceptValidationRuleFact Rule(string id, AdapterIdentity adapter, SubjectId subject) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Definition = new ConceptValidationRuleDefinition
        {
            Concept = subject,
            RuleIdentity = "account-number-format",
            Kind = ConceptValidationRuleKind.NamedPredicate,
            Predicate = "BeValidAccountNumber",
            Message = "Must be a valid account number",
            ImplementationFile = "Concepts/Validation/BeValidAccountNumber.cs"
        },
        Evidence = new Evidence { Adapter = adapter, Strength = EvidenceStrength.Exact }
    };
}
