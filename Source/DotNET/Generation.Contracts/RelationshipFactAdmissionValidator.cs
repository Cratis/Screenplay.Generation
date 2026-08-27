// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class RelationshipFactAdmissionValidator
{
    public static void Validate(
        RelationshipFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        var key = fact.Definition.Key;
        context.Enum(
            key.Kind,
            RelationshipKind.Unknown,
            $"{path}.Definition.Key.Kind",
            fact.Id,
            fact.Subject);
        AdapterContributionAdmissionValidator.ValidateSubject(
            key.Source,
            $"{path}.Definition.Key.Source",
            fact.Id,
            context);
        AdapterContributionAdmissionValidator.ValidateSubject(
            key.Target,
            $"{path}.Definition.Key.Target",
            fact.Id,
            context);

        if (key.Source != fact.Subject)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch,
                $"{path}.Definition.Key.Source",
                $"Relationship source '{key.Source.Value}' does not equal fact subject '{fact.Subject.Value}'",
                fact.Id,
                fact.Subject);
        }
    }
}
