// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Generation;

internal static class SpecificationSyntaxLowerer
{
    static readonly SourceLocation _generated = SourceLocation.Start;

    public static SpecificationSyntax[] Lower(
        ResolvedApplicationGraph graph,
        ArtifactPlacement placement,
        Func<ArtifactKey, string?> artifactName,
        LoweringSink sink)
    {
        var structuralPlacement = Structural.Placement(placement);
        var lowered = new List<SpecificationSyntax>();
        foreach (var scenario in graph.Specifications.Where(item =>
                     Structural.Placement(item.Placement) == structuralPlacement))
        {
            if (TryLower(scenario, artifactName) is { } specification)
            {
                lowered.Add(specification);
                Mark(scenario, sink.Coverage.Lowered);
            }
            else
            {
                var diagnostic = Unsupported(scenario);
                sink.Diagnostics.Add(diagnostic);
                Mark(scenario, key => sink.Coverage.Omitted(key, diagnostic));
            }
        }

        return [.. lowered.OrderBy(item => item.Name, StringComparer.Ordinal)];
    }

    static void Mark(
        AdmittedSpecificationScenario scenario,
        Action<string> mark)
    {
        mark(GenerationFactSemanticKey.SpecificationScenario(scenario.Definition));
        foreach (var step in scenario.Steps)
        {
            mark(GenerationFactSemanticKey.SpecificationStep(step.Definition));
            foreach (var value in step.Values)
            {
                Mark(value, mark);
            }
        }
    }

    static void Mark(
        AdmittedSpecificationValue value,
        Action<string> mark)
    {
        mark(GenerationFactSemanticKey.SpecificationValue(value.Definition));
        foreach (var child in value.Children)
        {
            Mark(child, mark);
        }
    }

    static SpecificationSyntax? TryLower(
        AdmittedSpecificationScenario scenario,
        Func<ArtifactKey, string?> artifactName)
    {
        if (!IsRepresentable(scenario))
        {
            return null;
        }

        var lowered = new LoweredSteps();
        foreach (var step in scenario.Steps)
        {
            var name = step.Definition.Artifact is null ? null : artifactName(step.Definition.Artifact);
            if (!lowered.TryAdd(step, name))
            {
                return null;
            }
        }

        return new(
            scenario.Definition.Name,
            lowered.GivenEvents,
            lowered.When,
            lowered.ThenEvents,
            lowered.ThenErrors,
            _generated,
            lowered.GivenReadModels,
            lowered.ThenReadModels)
        {
            File = FileFrom(scenario.Evidence),
            ThenQueries = lowered.ThenQueries
        };
    }

    static bool IsRepresentable(AdmittedSpecificationScenario scenario) =>
        scenario.Steps.SelectMany(step => step.Values).All(SpecificationValueSyntaxLowerer.IsScalar) &&
        !scenario.Steps.Any(HasErrorCode);

    static bool HasErrorCode(AdmittedSpecificationStep step) =>
        step.Definition.Kind == SpecificationStepKind.Error && step.Definition.ErrorCode is not null;

    static FileReferenceSyntax? FileFrom(IEnumerable<Evidence> evidence) =>
        evidence
            .Where(item => item.Source is not null)
            .OrderBy(Canonical.Evidence, StringComparer.Ordinal)
            .Select(item => new FileReferenceSyntax(item.Source!.Path, _generated))
            .FirstOrDefault();

    static GenerationDiagnostic Unsupported(AdmittedSpecificationScenario scenario) => new()
    {
        Code = GenerationDiagnosticCodes.UnsupportedSpecificationLowering,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Unsupported,
        Message = $"Specification scenario '{scenario.Definition.Name}' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole",
        Source = scenario.Evidence.Select(item => item.Source).FirstOrDefault(source => source is not null),
        Subject = scenario.Definition.Key.Scenario
    };

    sealed class LoweredSteps
    {
        public List<SpecificationEventSyntax> GivenEvents { get; } = [];

        public List<SpecificationReadModelSyntax> GivenReadModels { get; } = [];

        public SpecificationCommandSyntax? When { get; private set; }

        public List<SpecificationEventSyntax> ThenEvents { get; } = [];

        public List<SpecificationReadModelSyntax> ThenReadModels { get; } = [];

        public List<SpecificationQuerySyntax> ThenQueries { get; } = [];

        public List<SpecificationErrorSyntax> ThenErrors { get; } = [];

        public bool TryAdd(AdmittedSpecificationStep step, string? artifactName)
        {
            var definition = step.Definition;
            if (definition.Kind != SpecificationStepKind.Error && artifactName is null)
            {
                return false;
            }

            if ((definition.Phase, definition.Kind) == (SpecificationStepPhase.Then, SpecificationStepKind.Read))
            {
                return SpecificationValueSyntaxLowerer.TryLowerQuery(artifactName!, step.Values, ThenQueries);
            }

            return step.Values.All(value => value.Definition.Key.Path.Count == 1) &&
                Add(definition, artifactName, [.. step.Values.Select(SpecificationValueSyntaxLowerer.Mapping)]);
        }

        bool Add(SpecificationStepDefinition definition, string? artifactName, PropertyMappingSyntax[] mappings)
        {
            switch (definition.Phase, definition.Kind)
            {
                case (SpecificationStepPhase.Given, SpecificationStepKind.Event):
                    GivenEvents.Add(new(artifactName!, mappings, _generated));
                    return true;
                case (SpecificationStepPhase.Given, SpecificationStepKind.ReadModel):
                    GivenReadModels.Add(new(artifactName!, mappings, _generated));
                    return true;
                case (SpecificationStepPhase.When, SpecificationStepKind.Command):
                    When = new(artifactName!, mappings, _generated);
                    return true;
                case (SpecificationStepPhase.Then, SpecificationStepKind.Event):
                    ThenEvents.Add(new(artifactName!, mappings, _generated));
                    return true;
                case (SpecificationStepPhase.Then, SpecificationStepKind.ReadModel):
                    ThenReadModels.Add(new(artifactName!, mappings, _generated));
                    return true;
                case (SpecificationStepPhase.Then, SpecificationStepKind.Error):
                    ThenErrors.Add(new(definition.ErrorMessage, _generated));
                    return true;
                default:
                    return false;
            }
        }
    }
}
