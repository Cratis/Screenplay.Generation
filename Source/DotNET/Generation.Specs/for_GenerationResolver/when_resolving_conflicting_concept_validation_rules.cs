// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_conflicting_concept_validation_rules : given.facts
{
    ResolvedApplicationGraph _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Concepts.AccountNumber" };
        _result = new GenerationResolver().Resolve(
        [
            Contribution(FirstAdapter, Rule("first", FirstAdapter, subject, "BeValidAccountNumber")),
            Contribution(SecondAdapter, Rule("second", SecondAdapter, subject, "BeFormattedAccountNumber"))
        ]);
    }

    [Fact] void should_retain_both_rule_definitions() => _result.ConceptValidationRules.Single().Variants.Count.ShouldEqual(2);
    [Fact] void should_mark_the_rule_as_conflicted() => _result.ConceptValidationRules.Single().IsConflicted.ShouldBeTrue();
    [Fact] void should_report_the_conflict() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingConceptValidationRule);

    static ConceptValidationRuleFact Rule(string id, AdapterIdentity adapter, SubjectId subject, string predicate) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Definition = new ConceptValidationRuleDefinition
        {
            Concept = subject,
            RuleIdentity = "account-number-format",
            Kind = ConceptValidationRuleKind.NamedPredicate,
            Predicate = predicate
        },
        Evidence = new Evidence { Adapter = adapter, Strength = EvidenceStrength.Exact }
    };
}
