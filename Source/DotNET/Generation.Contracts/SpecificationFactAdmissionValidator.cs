// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Generation;

static class SpecificationFactAdmissionValidator
{
    public static void Validate(
        SpecificationScenarioFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        ValidateScenarioKey(definition.Key, $"{path}.Definition.Key", fact, context);
        if (definition.Key.Scenario != fact.Subject)
        {
            Ownership(
                fact,
                $"{path}.Definition.Key.Scenario",
                $"Scenario owner '{definition.Key.Scenario.Value}' does not equal fact subject '{fact.Subject.Value}'",
                context);
        }

        AdapterContributionAdmissionValidator.ValidateRequiredText(
            definition.Name,
            $"{path}.Definition.Name",
            fact.Id,
            fact.Subject,
            context);
        AdapterContributionAdmissionValidator.ValidateArtifactKey(
            definition.TargetArtifact,
            $"{path}.Definition.TargetArtifact",
            fact.Id,
            context);

        for (var index = 0; index < definition.Steps.Count; index++)
        {
            var step = definition.Steps[index];
            var stepPath = $"{path}.Definition.Steps[{index}]";
            ValidateStepKey(step, stepPath, fact, context);
            if (step.Scenario != definition.Key)
            {
                Ownership(fact, $"{stepPath}.Scenario", "Scenario step belongs to a different scenario", context);
            }

            if (step.Index != index)
            {
                Ownership(
                    fact,
                    $"{stepPath}.Index",
                    $"Scenario step at authored position '{index}' identifies position '{step.Index}'",
                    context);
            }
        }
    }

    public static void Validate(
        SpecificationStepFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        ValidateStepKey(definition.Key, $"{path}.Definition.Key", fact, context);
        context.Enum(
            definition.Phase,
            SpecificationStepPhase.Unknown,
            $"{path}.Definition.Phase",
            fact.Id,
            fact.Subject);
        context.Enum(
            definition.Kind,
            SpecificationStepKind.Unknown,
            $"{path}.Definition.Kind",
            fact.Id,
            fact.Subject);

        ValidateStepOperand(fact, path, context);
        for (var index = 0; index < definition.Values.Count; index++)
        {
            var value = definition.Values[index];
            var valuePath = $"{path}.Definition.Values[{index}]";
            ValidateValueKey(value, valuePath, fact, context);
            if (value.Step != definition.Key)
            {
                Ownership(fact, $"{valuePath}.Step", "Specification value belongs to a different step", context);
            }
        }

        ValidateUniqueValues(definition.Values, $"{path}.Definition.Values", fact, context);
    }

    public static void Validate(
        SpecificationValueFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        ValidateValueKey(definition.Key, $"{path}.Definition.Key", fact, context);
        context.Enum(
            definition.Kind,
            SpecificationValueKind.Unknown,
            $"{path}.Definition.Kind",
            fact.Id,
            fact.Subject);
        if (definition.Type is not null)
        {
            AdapterContributionAdmissionValidator.ValidateType(
                definition.Type,
                $"{path}.Definition.Type",
                fact.Id,
                fact.Subject,
                context);
        }

        ValidateValueOperand(fact, path, context);
        for (var index = 0; index < definition.Children.Count; index++)
        {
            var child = definition.Children[index];
            var childPath = $"{path}.Definition.Children[{index}]";
            ValidateValueKey(child, childPath, fact, context);
            if (child.Step != definition.Key.Step || !IsDirectChild(definition.Key.Path, child.Path))
            {
                Ownership(
                    fact,
                    childPath,
                    "Specification value child must belong to the same step and directly extend its parent path",
                    context);
            }
        }

        ValidateUniqueValues(definition.Children, $"{path}.Definition.Children", fact, context);
    }

    static void ValidateStepOperand(
        SpecificationStepFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        if (definition.Artifact is not null)
        {
            AdapterContributionAdmissionValidator.ValidateArtifactKey(
                definition.Artifact,
                $"{path}.Definition.Artifact",
                fact.Id,
                context);
        }

        if (definition.Kind == SpecificationStepKind.Error)
        {
            if (definition.Phase != SpecificationStepPhase.Then)
            {
                InvalidOperand(fact, $"{path}.Definition.Phase", "Error steps must use the Then phase", context);
            }

            if (definition.Artifact is not null)
            {
                InvalidOperand(fact, $"{path}.Definition.Artifact", "Error steps cannot carry an artifact operand", context);
            }

            if (definition.Values.Count > 0)
            {
                InvalidOperand(fact, $"{path}.Definition.Values", "Error steps cannot carry value operands", context);
            }
            return;
        }

        if (definition.Kind is SpecificationStepKind.Unknown || !Enum.IsDefined(definition.Kind))
        {
            return;
        }

        if (definition.Artifact is null)
        {
            InvalidOperand(
                fact,
                $"{path}.Definition.Artifact",
                $"{definition.Kind} steps require an artifact operand",
                context);
            return;
        }

        if (definition.ErrorCode is not null || definition.ErrorMessage is not null)
        {
            InvalidOperand(
                fact,
                $"{path}.Definition",
                $"{definition.Kind} steps cannot carry error operands",
                context);
        }

        var validKind = definition.Kind switch
        {
            SpecificationStepKind.Event => definition.Artifact.Kind == ArtifactKind.Event,
            SpecificationStepKind.ReadModel => definition.Artifact.Kind == ArtifactKind.ReadModel,
            SpecificationStepKind.Command => definition.Artifact.Kind == ArtifactKind.Command,
            SpecificationStepKind.Read => definition.Artifact.Kind == ArtifactKind.Query,
            _ => false
        };
        if (!validKind)
        {
            InvalidOperand(
                fact,
                $"{path}.Definition.Artifact.Kind",
                $"Artifact kind '{definition.Artifact.Kind}' is not valid for a '{definition.Kind}' specification step",
                context);
        }
    }

    static void ValidateValueOperand(
        SpecificationValueFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        switch (definition.Kind)
        {
            case SpecificationValueKind.Null:
                if (definition.Scalar is not null || definition.Children.Count > 0)
                {
                    InvalidOperand(fact, $"{path}.Definition", "Null values cannot carry scalar or child operands", context);
                }
                break;
            case SpecificationValueKind.Text:
                if (definition.Scalar is null)
                {
                    InvalidOperand(fact, $"{path}.Definition.Scalar", "Text values require a scalar operand", context);
                }

                RejectScalarChildren(fact, path, context);
                break;
            case SpecificationValueKind.Number:
                if (!decimal.TryParse(definition.Scalar, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                {
                    InvalidOperand(fact, $"{path}.Definition.Scalar", "Number values require a valid invariant decimal scalar operand", context);
                }

                RejectScalarChildren(fact, path, context);
                break;
            case SpecificationValueKind.Boolean:
                if (definition.Scalar is not ("true" or "false"))
                {
                    InvalidOperand(fact, $"{path}.Definition.Scalar", "Boolean values require the lowercase scalar 'true' or 'false'", context);
                }

                RejectScalarChildren(fact, path, context);
                break;
            case SpecificationValueKind.Collection:
            case SpecificationValueKind.Composite:
                if (definition.Scalar is not null)
                {
                    InvalidOperand(fact, $"{path}.Definition.Scalar", $"{definition.Kind} values cannot carry a scalar operand", context);
                }
                break;
        }
    }

    static void RejectScalarChildren(
        SpecificationValueFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (fact.Definition.Children.Count > 0)
        {
            InvalidOperand(fact, $"{path}.Definition.Children", $"{fact.Definition.Kind} values cannot carry child operands", context);
        }
    }

    static void ValidateScenarioKey(
        SpecificationScenarioKey key,
        string path,
        GenerationFact fact,
        AdapterContributionAdmissionContext context) =>
        AdapterContributionAdmissionValidator.ValidateSubject(key.Scenario, $"{path}.Scenario", fact.Id, context);

    static void ValidateStepKey(
        SpecificationStepKey key,
        string path,
        GenerationFact fact,
        AdapterContributionAdmissionContext context)
    {
        ValidateScenarioKey(key.Scenario, $"{path}.Scenario", fact, context);
        if (key.Index < 0)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand,
                $"{path}.Index",
                "Specification step index must be nonnegative",
                fact.Id,
                fact.Subject);
        }
    }

    static void ValidateValueKey(
        SpecificationValueKey key,
        string path,
        GenerationFact fact,
        AdapterContributionAdmissionContext context)
    {
        ValidateStepKey(key.Step, $"{path}.Step", fact, context);
        for (var index = 0; index < key.Path.Count; index++)
        {
            AdapterContributionAdmissionValidator.ValidateRequiredText(
                key.Path[index],
                $"{path}.Path[{index}]",
                fact.Id,
                fact.Subject,
                context);
        }
    }

    static void ValidateUniqueValues(
        IReadOnlyList<SpecificationValueKey> values,
        string path,
        GenerationFact fact,
        AdapterContributionAdmissionContext context)
    {
        foreach (var duplicate in values
                     .GroupBy(ValueKey, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            Ownership(fact, path, $"Specification value key '{duplicate.Key}' occurs more than once", context);
        }
    }

    static bool IsDirectChild(IReadOnlyList<string> parent, IReadOnlyList<string> child) =>
        child.Count == parent.Count + 1 && parent.SequenceEqual(child.Take(parent.Count), StringComparer.Ordinal);

    static string ValueKey(SpecificationValueKey value) =>
        $"{value.Step.Scenario.Scenario.Value}\u001f{value.Step.Index}\u001f{string.Join('\u001f', value.Path)}";

    static void Ownership(
        GenerationFact fact,
        string path,
        string message,
        AdapterContributionAdmissionContext context) =>
        context.Add(
            AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch,
            path,
            message,
            fact.Id,
            fact.Subject);

    static void InvalidOperand(
        GenerationFact fact,
        string path,
        string message,
        AdapterContributionAdmissionContext context) =>
        context.Add(
            AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand,
            path,
            message,
            fact.Id,
            fact.Subject);
}
