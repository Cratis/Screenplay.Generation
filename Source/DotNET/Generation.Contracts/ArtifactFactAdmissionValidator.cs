// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ArtifactFactAdmissionValidator
{
    public static void Validate(
        ArtifactFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        AdapterContributionAdmissionValidator.ValidateArtifactKey(
            definition.Key,
            $"{path}.Definition.Key",
            fact.Id,
            context);
        ValidateOwner(fact, definition.Key.Subject, $"{path}.Definition.Key.Subject", context);
        AdapterContributionAdmissionValidator.ValidateRequiredText(
            definition.Name,
            $"{path}.Definition.Name",
            fact.Id,
            fact.Subject,
            context);

        for (var index = 0; index < definition.Properties.Count; index++)
        {
            var property = definition.Properties[index];
            var propertyPath = $"{path}.Definition.Properties[{index}]";
            AdapterContributionAdmissionValidator.ValidateRequiredText(
                property.Name,
                $"{propertyPath}.Name",
                fact.Id,
                fact.Subject,
                context);
            AdapterContributionAdmissionValidator.ValidateType(
                property.Type,
                $"{propertyPath}.Type",
                fact.Id,
                fact.Subject,
                context);
        }

        foreach (var duplicate in definition.Properties
                     .Where(property => !string.IsNullOrWhiteSpace(property.Name))
                     .GroupBy(property => property.Name, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch,
                $"{path}.Definition.Properties",
                $"Artifact property name '{duplicate.Key}' occurs more than once",
                fact.Id,
                fact.Subject);
        }
    }

    public static void Validate(
        ArtifactPlacementFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        AdapterContributionAdmissionValidator.ValidateArtifactKey(
            fact.Artifact,
            $"{path}.Artifact",
            fact.Id,
            context);
        ValidateOwner(fact, fact.Artifact.Subject, $"{path}.Artifact.Subject", context);
        AdapterContributionAdmissionValidator.ValidateRequiredText(
            fact.Placement.Module,
            $"{path}.Placement.Module",
            fact.Id,
            fact.Subject,
            context);
        AdapterContributionAdmissionValidator.ValidateRequiredText(
            fact.Placement.Slice,
            $"{path}.Placement.Slice",
            fact.Id,
            fact.Subject,
            context);
        context.Enum(
            fact.Placement.SliceKind,
            GenerationSliceKind.Unknown,
            $"{path}.Placement.SliceKind",
            fact.Id,
            fact.Subject);

        for (var index = 0; index < fact.Placement.Features.Count; index++)
        {
            AdapterContributionAdmissionValidator.ValidateRequiredText(
                fact.Placement.Features[index],
                $"{path}.Placement.Features[{index}]",
                fact.Id,
                fact.Subject,
                context);
        }
    }

    static void ValidateOwner(
        GenerationFact fact,
        SubjectId owner,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (owner != fact.Subject)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch,
                path,
                $"Nested artifact owner '{owner.Value}' does not equal fact subject '{fact.Subject.Value}'",
                fact.Id,
                fact.Subject);
        }
    }
}
