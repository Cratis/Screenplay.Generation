// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

internal static class SpecificationAdmission
{
    public static AdmittedSpecificationScenario[] Admit(
        ResolvedSpecificationFacts facts,
        IReadOnlyList<ResolvedArtifact> artifacts,
        IReadOnlyList<ResolvedArtifactPlacement> placements,
        ICollection<GenerationDiagnostic> diagnostics)
    {
        var artifactsByKey = artifacts
            .Where(artifact => !artifact.IsConflicted)
            .ToDictionary(artifact => Canonical.ArtifactKey(artifact.Key), StringComparer.Ordinal);
        var placementsByKey = placements
            .Where(placement => !placement.IsConflicted)
            .ToDictionary(placement => Canonical.ArtifactKey(placement.Artifact), StringComparer.Ordinal);
        var stepsByKey = facts.Steps.ToDictionary(step => Canonical.SpecificationStepKey(step.Key), StringComparer.Ordinal);
        var valueAdmission = new SpecificationValueAdmission(facts.Values);
        var admitted = new List<AdmittedSpecificationScenario>();

        foreach (var scenario in facts.Scenarios)
        {
            if (TryAdmit(
                    scenario,
                    stepsByKey,
                    valueAdmission,
                    artifactsByKey,
                    placementsByKey,
                    out var admittedScenario))
            {
                admitted.Add(admittedScenario!);
                continue;
            }

            diagnostics.Add(Incomplete(scenario));
        }

        return [.. admitted];
    }

    static bool TryAdmit(
        ResolvedSpecificationScenario scenario,
        Dictionary<string, ResolvedSpecificationStep> steps,
        SpecificationValueAdmission values,
        Dictionary<string, ResolvedArtifact> artifacts,
        Dictionary<string, ResolvedArtifactPlacement> placements,
        out AdmittedSpecificationScenario? admitted)
    {
        admitted = null;
        if (scenario.IsConflicted)
        {
            return false;
        }

        var variant = scenario.Variants.Single();
        var definition = variant.Definition;
        var targetKey = Canonical.ArtifactKey(definition.TargetArtifact);
        if (!artifacts.ContainsKey(targetKey) || !placements.TryGetValue(targetKey, out var placement) ||
            placement.EffectiveVariants.Count != 1 || !ValidStepKeys(definition))
        {
            return false;
        }

        var admittedSteps = new List<AdmittedSpecificationStep>();
        foreach (var stepKey in definition.Steps)
        {
            if (!steps.TryGetValue(Canonical.SpecificationStepKey(stepKey), out var step) ||
                !TryAdmitStep(step, values, artifacts, out var admittedStep))
            {
                return false;
            }

            admittedSteps.Add(admittedStep!);
        }

        if (!ValidPhases(admittedSteps))
        {
            return false;
        }

        admitted = new()
        {
            Definition = definition,
            Placement = placement.EffectiveVariants.Single().Placement,
            Evidence = variant.Evidence,
            Steps = admittedSteps
        };
        return true;
    }

    static bool TryAdmitStep(
        ResolvedSpecificationStep step,
        SpecificationValueAdmission values,
        Dictionary<string, ResolvedArtifact> artifacts,
        out AdmittedSpecificationStep? admitted)
    {
        admitted = null;
        if (step.IsConflicted)
        {
            return false;
        }

        var variant = step.Variants.Single();
        var definition = variant.Definition;
        if (!ValidStepShape(definition, artifacts) ||
            definition.Values.Select(Canonical.SpecificationValueKey).Distinct(StringComparer.Ordinal).Count() != definition.Values.Count)
        {
            return false;
        }

        var admittedValues = new List<AdmittedSpecificationValue>();
        foreach (var value in definition.Values)
        {
            if (!values.TryAdmit(value, definition.Key, out var admittedValue))
            {
                return false;
            }

            admittedValues.Add(admittedValue!);
        }

        admitted = new()
        {
            Definition = definition,
            Evidence = variant.Evidence,
            Values = admittedValues
        };
        return true;
    }

    static bool ValidStepKeys(SpecificationScenarioDefinition definition) =>
        definition.Steps.Count > 0 &&
        definition.Steps.Select(Canonical.SpecificationStepKey).Distinct(StringComparer.Ordinal).Count() == definition.Steps.Count &&
        definition.Steps.Select((step, index) => step.Index == index &&
            Canonical.SpecificationScenarioKey(step.Scenario) == Canonical.SpecificationScenarioKey(definition.Key)).All(valid => valid);

    static bool ValidStepShape(
        SpecificationStepDefinition step,
        Dictionary<string, ResolvedArtifact> artifacts)
    {
        if (step.Kind == SpecificationStepKind.Error)
        {
            return step.Phase == SpecificationStepPhase.Then && step.Artifact is null && step.Values.Count == 0;
        }

        if (step.Artifact is null || step.ErrorCode is not null || step.ErrorMessage is not null ||
            !artifacts.TryGetValue(Canonical.ArtifactKey(step.Artifact), out var artifact))
        {
            return false;
        }

        var expectedKind = step.Kind switch
        {
            SpecificationStepKind.Event => ArtifactKind.Event,
            SpecificationStepKind.ReadModel => ArtifactKind.ReadModel,
            SpecificationStepKind.Command => ArtifactKind.Command,
            SpecificationStepKind.Read => ArtifactKind.Query,
            _ => ArtifactKind.Unknown
        };
        return artifact.Key.Kind == expectedKind;
    }

    static bool ValidPhases(IReadOnlyList<AdmittedSpecificationStep> steps)
    {
        var phases = steps.Select(step => step.Definition.Phase).ToArray();
        return phases.Count(phase => phase == SpecificationStepPhase.When) <= 1 &&
            phases.Any(phase => phase == SpecificationStepPhase.Then) &&
            phases.SequenceEqual(phases.OrderBy(phase => (int)phase));
    }

    static SourceRange? FirstSource(IEnumerable<Evidence> evidence) =>
        evidence
            .Where(item => item.Source is not null)
            .OrderBy(Canonical.Evidence, StringComparer.Ordinal)
            .Select(item => item.Source)
            .FirstOrDefault();

    static GenerationDiagnostic Incomplete(ResolvedSpecificationScenario scenario) => new()
    {
        Code = GenerationDiagnosticCodes.IncompleteSpecificationScenario,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Unsupported,
        Message = $"Specification scenario '{scenario.Key.Scenario.Value}' could not be represented completely; no partial scenario was admitted",
        Source = FirstSource(scenario.Variants.SelectMany(variant => variant.Evidence)),
        Subject = scenario.Key.Scenario
    };
}
