// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class BackendScopeFactAdmissionValidator
{
    public static void Validate(BackendScopeFact fact, string path, AdapterContributionAdmissionContext context)
    {
        ValidateKey(fact.Definition.Key, $"{path}.Definition.Key", fact.Id, fact.Subject, context);
        if (fact.Definition.Key.Store != fact.Subject)
        {
            context.Add(AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch, $"{path}.Definition.Key.Store", "The backend scope store must equal the fact subject", fact.Id, fact.Subject);
        }

        if (fact.Evidence.Source is null)
        {
            context.Add(AdapterContributionAdmissionDiagnosticCode.MissingRequiredValue, $"{path}.Evidence.Source", "A backend scope declaration requires an authored source location", fact.Id, fact.Subject);
        }
    }

    public static void ValidateKey(BackendScopeKey? key, string path, FactId fact, SubjectId subject, AdapterContributionAdmissionContext context)
    {
        if (key is null)
        {
            return;
        }

        AdapterContributionAdmissionValidator.ValidateSubject(key.Store, $"{path}.Store", fact, context);
        if (key.EventModelName is { } name && (string.IsNullOrWhiteSpace(name) || name.Any(char.IsControl)))
        {
            context.Add(AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand, $"{path}.EventModelName", "An event-model qualifier must be nonempty text without control characters", fact, subject);
        }
    }

    public static void ValidateObserved(BackendScopeKey? scope, SubjectId? observed, string path, GenerationFact fact, AdapterContributionAdmissionContext context)
    {
        ValidateKey(scope, path, fact.Id, fact.Subject, context);
        if (scope is not null && observed is null)
        {
            context.Add(AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand, path, "A target scope requires an exact target subject", fact.Id, fact.Subject);
        }
    }
}
