// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

internal static class SpecificationAdmission
{
    static readonly Dictionary<SpecificationStepKind, ArtifactKind> _artifactKinds = new()
    {
        [SpecificationStepKind.Event] = ArtifactKind.Event,
        [SpecificationStepKind.ReadModel] = ArtifactKind.ReadModel,
        [SpecificationStepKind.Command] = ArtifactKind.Command,
        [SpecificationStepKind.Read] = ArtifactKind.Query
    };

    public static AdmittedSpecificationScenario[] Admit(
        ResolvedSpecificationFacts facts,
        IReadOnlyList<ResolvedArtifact> artifacts,
        IReadOnlyList<ResolvedArtifactPlacement> placements,
        ICollection<GenerationDiagnostic> diagnostics)
    {
        var index = new AdmissionIndex(
            artifacts
                .Where(artifact => !artifact.IsConflicted)
                .ToDictionary(artifact => Structural.ArtifactKey(artifact.Key), StringComparer.Ordinal),
            placements
                .Where(placement => !placement.IsConflicted)
                .ToDictionary(placement => Structural.ArtifactKey(placement.Artifact), StringComparer.Ordinal),
            facts.Steps.ToDictionary(step => Structural.SpecificationStepKey(step.Key), StringComparer.Ordinal),
            new SpecificationValueAdmission(facts.Values));
        var admitted = new List<AdmittedSpecificationScenario>();

        foreach (var scenario in facts.Scenarios)
        {
            if (TryAdmit(scenario, index) is { } admittedScenario)
            {
                admitted.Add(admittedScenario);
                continue;
            }

            diagnostics.Add(Incomplete(scenario));
        }

        return [.. admitted];
    }

    static AdmittedSpecificationScenario? TryAdmit(ResolvedSpecificationScenario scenario, AdmissionIndex index)
    {
        if (scenario.IsConflicted)
        {
            return null;
        }

        var variant = scenario.Variants.Single();
        var definition = variant.Definition;
        var targetKey = Structural.ArtifactKey(definition.TargetArtifact);
        if (!index.Artifacts.ContainsKey(targetKey) ||
            !index.Placements.TryGetValue(targetKey, out var placement) ||
            placement.EffectiveVariants.Count != 1 ||
            !ValidStepKeys(definition))
        {
            return null;
        }

        var admittedSteps = AdmitSteps(definition, index);
        return admittedSteps is not null && ValidPhases(admittedSteps)
            ? new()
            {
                Definition = definition,
                Placement = placement.EffectiveVariants.Single().Placement,
                Evidence = variant.Evidence,
                Steps = admittedSteps
            }
            : null;
    }

    static List<AdmittedSpecificationStep>? AdmitSteps(SpecificationScenarioDefinition definition, AdmissionIndex index)
    {
        var admittedSteps = new List<AdmittedSpecificationStep>();
        foreach (var stepKey in definition.Steps)
        {
            if (!index.Steps.TryGetValue(Structural.SpecificationStepKey(stepKey), out var step) ||
                TryAdmitStep(step, index) is not { } admittedStep)
            {
                return null;
            }

            admittedSteps.Add(admittedStep);
        }

        return admittedSteps;
    }

    static AdmittedSpecificationStep? TryAdmitStep(ResolvedSpecificationStep step, AdmissionIndex index)
    {
        if (step.IsConflicted)
        {
            return null;
        }

        var variant = step.Variants.Single();
        var definition = variant.Definition;
        if (!ValidStepShape(definition, index.Artifacts) ||
            definition.Values.Select(Structural.SpecificationValueKey).Distinct(StringComparer.Ordinal).Count() != definition.Values.Count)
        {
            return null;
        }

        var admittedValues = new List<AdmittedSpecificationValue>();
        foreach (var value in definition.Values)
        {
            if (!index.Values.TryAdmit(value, definition.Key, out var admittedValue))
            {
                return null;
            }

            admittedValues.Add(admittedValue!);
        }

        return new()
        {
            Definition = definition,
            Evidence = variant.Evidence,
            Values = admittedValues
        };
    }

    static bool ValidStepKeys(SpecificationScenarioDefinition definition) =>
        definition.Steps.Count > 0 &&
        definition.Steps.Select(Structural.SpecificationStepKey).Distinct(StringComparer.Ordinal).Count() == definition.Steps.Count &&
        definition.Steps.Select((step, index) => step.Index == index &&
            Structural.SpecificationScenarioKey(step.Scenario) == Structural.SpecificationScenarioKey(definition.Key)).All(valid => valid);

    static bool ValidStepShape(
        SpecificationStepDefinition step,
        Dictionary<string, ResolvedArtifact> artifacts) =>
        step.Kind == SpecificationStepKind.Error
            ? IsValidErrorStep(step)
            : HasNoErrorData(step) &&
              step.Artifact is not null &&
              artifacts.TryGetValue(Structural.ArtifactKey(step.Artifact), out var artifact) &&
              artifact.Key.Kind == ExpectedArtifactKind(step.Kind);

    static bool IsValidErrorStep(SpecificationStepDefinition step) =>
        step.Phase == SpecificationStepPhase.Then && step.Artifact is null && step.Values.Count == 0;

    static bool HasNoErrorData(SpecificationStepDefinition step) =>
        step.ErrorCode is null && step.ErrorMessage is null;

    static ArtifactKind ExpectedArtifactKind(SpecificationStepKind kind) =>
        _artifactKinds.GetValueOrDefault(kind, ArtifactKind.Unknown);

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

    sealed record AdmissionIndex(
        Dictionary<string, ResolvedArtifact> Artifacts,
        Dictionary<string, ResolvedArtifactPlacement> Placements,
        Dictionary<string, ResolvedSpecificationStep> Steps,
        SpecificationValueAdmission Values);
}
