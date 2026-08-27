// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

internal sealed record ResolvedSpecificationFacts(
    ResolvedSpecificationScenario[] Scenarios,
    ResolvedSpecificationStep[] Steps,
    ResolvedSpecificationValue[] Values);

internal static class SpecificationFactResolver
{
    public static ResolvedSpecificationFacts Resolve(
        IEnumerable<SpecificationScenarioFact> scenarioFacts,
        IEnumerable<SpecificationStepFact> stepFacts,
        IEnumerable<SpecificationValueFact> valueFacts,
        List<GenerationDiagnostic> diagnostics)
    {
        var scenarios = ResolveScenarios(
            scenarioFacts.Where(fact => ValidateScenarioFact(fact, diagnostics)),
            diagnostics);
        var steps = ResolveSteps(stepFacts, diagnostics);
        var values = ResolveValues(valueFacts, diagnostics);
        return new(scenarios, steps, values);
    }

    static ResolvedSpecificationScenario[] ResolveScenarios(
        IEnumerable<SpecificationScenarioFact> facts,
        List<GenerationDiagnostic> diagnostics) =>
        [
            .. facts
                .GroupBy(fact => Structural.SpecificationScenarioKey(fact.Definition.Key), StringComparer.Ordinal)
                .OrderBy(group => Canonical.SpecificationScenarioKey(group.First().Definition.Key), StringComparer.Ordinal)
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var variants = group
                        .GroupBy(fact => Structural.SpecificationScenario(fact.Definition), StringComparer.Ordinal)
                        .OrderBy(variant => Canonical.SpecificationScenario(variant.First().Definition), StringComparer.Ordinal)
                        .ThenBy(variant => variant.Key, StringComparer.Ordinal)
                        .Select(variant => new ResolvedSpecificationScenarioVariant
                        {
                            Definition = variant.First().Definition,
                            Evidence = OrderedEvidence(variant.Select(fact => fact.Evidence))
                        })
                        .ToArray();
                    var scenario = new ResolvedSpecificationScenario
                    {
                        Key = variants[0].Definition.Key,
                        Variants = variants
                    };
                    if (scenario.IsConflicted)
                    {
                        diagnostics.Add(ConflictFor(scenario));
                    }

                    return scenario;
                })
        ];

    static ResolvedSpecificationStep[] ResolveSteps(
        IEnumerable<SpecificationStepFact> facts,
        List<GenerationDiagnostic> diagnostics) =>
        [
            .. facts
                .GroupBy(fact => Structural.SpecificationStepKey(fact.Definition.Key), StringComparer.Ordinal)
                .OrderBy(group => Canonical.SpecificationStepKey(group.First().Definition.Key), StringComparer.Ordinal)
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var variants = group
                        .GroupBy(fact => Structural.SpecificationStep(fact.Definition), StringComparer.Ordinal)
                        .OrderBy(variant => Canonical.SpecificationStep(variant.First().Definition), StringComparer.Ordinal)
                        .ThenBy(variant => variant.Key, StringComparer.Ordinal)
                        .Select(variant => new ResolvedSpecificationStepVariant
                        {
                            Definition = variant.First().Definition,
                            Evidence = OrderedEvidence(variant.Select(fact => fact.Evidence))
                        })
                        .ToArray();
                    var step = new ResolvedSpecificationStep
                    {
                        Key = variants[0].Definition.Key,
                        Variants = variants
                    };
                    if (step.IsConflicted)
                    {
                        diagnostics.Add(ConflictFor(step));
                    }

                    return step;
                })
        ];

    static ResolvedSpecificationValue[] ResolveValues(
        IEnumerable<SpecificationValueFact> facts,
        List<GenerationDiagnostic> diagnostics) =>
        [
            .. facts
                .GroupBy(fact => Structural.SpecificationValueKey(fact.Definition.Key), StringComparer.Ordinal)
                .OrderBy(group => Canonical.SpecificationValueKey(group.First().Definition.Key), StringComparer.Ordinal)
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var variants = group
                        .GroupBy(fact => Structural.SpecificationValue(fact.Definition), StringComparer.Ordinal)
                        .OrderBy(variant => Canonical.SpecificationValue(variant.First().Definition), StringComparer.Ordinal)
                        .ThenBy(variant => variant.Key, StringComparer.Ordinal)
                        .Select(variant => new ResolvedSpecificationValueVariant
                        {
                            Definition = variant.First().Definition,
                            Evidence = OrderedEvidence(variant.Select(fact => fact.Evidence))
                        })
                        .ToArray();
                    var value = new ResolvedSpecificationValue
                    {
                        Key = variants[0].Definition.Key,
                        Variants = variants
                    };
                    if (value.IsConflicted)
                    {
                        diagnostics.Add(ConflictFor(value));
                    }

                    return value;
                })
        ];

    static bool ValidateScenarioFact(
        SpecificationScenarioFact fact,
        List<GenerationDiagnostic> diagnostics)
    {
        if (fact.Subject == fact.Definition.Key.Scenario)
        {
            return true;
        }

        diagnostics.Add(new GenerationDiagnostic
        {
            Code = GenerationDiagnosticCodes.InvalidSpecificationFact,
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = GenerationDiagnosticOutcome.Unsupported,
            Message = $"Specification scenario fact '{fact.Id.Value}' targets '{fact.Definition.Key.Scenario.Value}' but asserts subject '{fact.Subject.Value}'",
            Source = fact.Evidence.Source,
            Subject = fact.Subject
        });
        return false;
    }

    static Evidence[] OrderedEvidence(IEnumerable<Evidence> evidence) =>
        [.. evidence
            .GroupBy(Canonical.Evidence, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.First())];

    static SourceRange? FirstSource(IEnumerable<Evidence> evidence) =>
        evidence
            .Where(item => item.Source is not null)
            .OrderBy(Canonical.Evidence, StringComparer.Ordinal)
            .Select(item => item.Source)
            .FirstOrDefault();

    static GenerationDiagnostic ConflictFor(ResolvedSpecificationScenario scenario) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingSpecificationScenario,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Conflict,
        Message = $"Specification scenario '{scenario.Key.Scenario.Value}' has {scenario.Variants.Count} incompatible definitions",
        Source = FirstSource(scenario.Variants.SelectMany(variant => variant.Evidence)),
        Subject = scenario.Key.Scenario
    };

    static GenerationDiagnostic ConflictFor(ResolvedSpecificationStep step) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingSpecificationStep,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Conflict,
        Message = $"Specification step '{step.Key.Index}' in '{step.Key.Scenario.Scenario.Value}' has {step.Variants.Count} incompatible definitions",
        Source = FirstSource(step.Variants.SelectMany(variant => variant.Evidence)),
        Subject = step.Key.Scenario.Scenario
    };

    static GenerationDiagnostic ConflictFor(ResolvedSpecificationValue value) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingSpecificationValue,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Conflict,
        Message = $"Specification value '{string.Join('.', value.Key.Path)}' in step '{value.Key.Step.Index}' has {value.Variants.Count} incompatible definitions",
        Source = FirstSource(value.Variants.SelectMany(variant => variant.Evidence)),
        Subject = value.Key.Step.Scenario.Scenario
    };
}
