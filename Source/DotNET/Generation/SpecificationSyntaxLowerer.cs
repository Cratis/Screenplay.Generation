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
        ICollection<GenerationDiagnostic> diagnostics,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        var structuralPlacement = Structural.Placement(placement);
        var lowered = new List<SpecificationSyntax>();
        foreach (var scenario in graph.Specifications.Where(item =>
                     Structural.Placement(item.Placement) == structuralPlacement))
        {
            if (TryLower(scenario, artifactName, out var specification))
            {
                lowered.Add(specification!);
                MarkLowered(scenario, coverage);
            }
            else
            {
                var diagnostic = Unsupported(scenario);
                diagnostics.Add(diagnostic);
                MarkOmitted(scenario, diagnostic, coverage);
            }
        }

        return [.. lowered.OrderBy(item => item.Name, StringComparer.Ordinal)];
    }

    static void MarkLowered(
        AdmittedSpecificationScenario scenario,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        coverage.Lowered(GenerationFactSemanticKey.SpecificationScenario(scenario.Definition));
        foreach (var step in scenario.Steps)
        {
            coverage.Lowered(GenerationFactSemanticKey.SpecificationStep(step.Definition));
            foreach (var value in step.Values)
            {
                MarkLowered(value, coverage);
            }
        }
    }

    static void MarkLowered(
        AdmittedSpecificationValue value,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        coverage.Lowered(GenerationFactSemanticKey.SpecificationValue(value.Definition));
        foreach (var child in value.Children)
        {
            MarkLowered(child, coverage);
        }
    }

    static void MarkOmitted(
        AdmittedSpecificationScenario scenario,
        GenerationDiagnostic diagnostic,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        coverage.Omitted(GenerationFactSemanticKey.SpecificationScenario(scenario.Definition), diagnostic);
        foreach (var step in scenario.Steps)
        {
            coverage.Omitted(GenerationFactSemanticKey.SpecificationStep(step.Definition), diagnostic);
            foreach (var value in step.Values)
            {
                MarkOmitted(value, diagnostic, coverage);
            }
        }
    }

    static void MarkOmitted(
        AdmittedSpecificationValue value,
        GenerationDiagnostic diagnostic,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        coverage.Omitted(GenerationFactSemanticKey.SpecificationValue(value.Definition), diagnostic);
        foreach (var child in value.Children)
        {
            MarkOmitted(child, diagnostic, coverage);
        }
    }

    static bool TryLower(
        AdmittedSpecificationScenario scenario,
        Func<ArtifactKey, string?> artifactName,
        out SpecificationSyntax? specification)
    {
        specification = null;
        if (scenario.Steps.SelectMany(step => step.Values).Any(value => !SpecificationValueSyntaxLowerer.IsScalar(value)) ||
            scenario.Steps.Any(step => step.Definition.Kind == SpecificationStepKind.Error &&
                step.Definition.ErrorCode is not null))
        {
            return false;
        }

        var givenEvents = new List<SpecificationEventSyntax>();
        var givenReadModels = new List<SpecificationReadModelSyntax>();
        SpecificationCommandSyntax? when = null;
        var thenEvents = new List<SpecificationEventSyntax>();
        var thenReadModels = new List<SpecificationReadModelSyntax>();
        var thenQueries = new List<SpecificationQuerySyntax>();
        var thenErrors = new List<SpecificationErrorSyntax>();

        foreach (var step in scenario.Steps)
        {
            var name = step.Definition.Artifact is null ? null : artifactName(step.Definition.Artifact);
            if (!TryLowerStep(
                    step,
                    name,
                    givenEvents,
                    givenReadModels,
                    ref when,
                    thenEvents,
                    thenReadModels,
                    thenQueries,
                    thenErrors))
            {
                return false;
            }
        }

        specification = new(
            scenario.Definition.Name,
            givenEvents,
            when,
            thenEvents,
            thenErrors,
            _generated,
            givenReadModels,
            thenReadModels)
        {
            File = FileFrom(scenario.Evidence),
            ThenQueries = thenQueries
        };
        return true;
    }

    static bool TryLowerStep(
        AdmittedSpecificationStep step,
        string? artifactName,
        List<SpecificationEventSyntax> givenEvents,
        List<SpecificationReadModelSyntax> givenReadModels,
        ref SpecificationCommandSyntax? when,
        List<SpecificationEventSyntax> thenEvents,
        List<SpecificationReadModelSyntax> thenReadModels,
        List<SpecificationQuerySyntax> thenQueries,
        List<SpecificationErrorSyntax> thenErrors)
    {
        var definition = step.Definition;
        if (definition.Kind != SpecificationStepKind.Error && artifactName is null)
        {
            return false;
        }

        if ((definition.Phase, definition.Kind) == (SpecificationStepPhase.Then, SpecificationStepKind.Read))
        {
            return SpecificationValueSyntaxLowerer.TryLowerQuery(artifactName!, step.Values, thenQueries);
        }

        if (step.Values.Any(value => value.Definition.Key.Path.Count != 1))
        {
            return false;
        }

        var mappings = step.Values.Select(SpecificationValueSyntaxLowerer.Mapping).ToArray();
        switch (definition.Phase, definition.Kind)
        {
            case (SpecificationStepPhase.Given, SpecificationStepKind.Event):
                givenEvents.Add(new(artifactName!, mappings, _generated));
                return true;
            case (SpecificationStepPhase.Given, SpecificationStepKind.ReadModel):
                givenReadModels.Add(new(artifactName!, mappings, _generated));
                return true;
            case (SpecificationStepPhase.When, SpecificationStepKind.Command):
                when = new(artifactName!, mappings, _generated);
                return true;
            case (SpecificationStepPhase.Then, SpecificationStepKind.Event):
                thenEvents.Add(new(artifactName!, mappings, _generated));
                return true;
            case (SpecificationStepPhase.Then, SpecificationStepKind.ReadModel):
                thenReadModels.Add(new(artifactName!, mappings, _generated));
                return true;
            case (SpecificationStepPhase.Then, SpecificationStepKind.Error):
                thenErrors.Add(new(definition.ErrorMessage, _generated));
                return true;
            default:
                return false;
        }
    }

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
}
