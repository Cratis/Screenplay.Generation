// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class BackendScopeFactAdmissionValidator
{
    public static void Validate(BackendScopeFact fact, string path, AdapterContributionAdmissionContext context)
    {
        ValidateKey(fact.Definition.Key, $"{path}.Definition.Key", (fact.Id, fact.Subject), context);
        if (fact.Definition.Key.Store != fact.Subject)
        {
            context.Add(AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch, $"{path}.Definition.Key.Store", "The backend scope store must equal the fact subject", fact.Id, fact.Subject);
        }

        if (fact.Evidence.Source is null)
        {
            context.Add(AdapterContributionAdmissionDiagnosticCode.MissingRequiredValue, $"{path}.Evidence.Source", "A backend scope declaration requires an authored source location", fact.Id, fact.Subject);
        }
    }

    public static void ValidateKey(BackendScopeKey? key, string path, (FactId Id, SubjectId Subject) owner, AdapterContributionAdmissionContext context)
    {
        if (key is null)
        {
            return;
        }

        AdapterContributionAdmissionValidator.ValidateSubject(key.Store, $"{path}.Store", owner.Id, context);
        if (key.EventModelName is { } name && InvalidQualifier(name))
        {
            context.Add(AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand, $"{path}.EventModelName", "An event-model qualifier must be nonempty text without control characters", owner.Id, owner.Subject);
        }
    }

    public static void ValidateObserved((BackendScopeKey? Scope, SubjectId? Subject) target, string path, GenerationFact fact, AdapterContributionAdmissionContext context)
    {
        ValidateKey(target.Scope, path, (fact.Id, fact.Subject), context);
        if (target.Scope is not null && target.Subject is null)
        {
            context.Add(AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand, path, "A target scope requires an exact target subject", fact.Id, fact.Subject);
        }
    }

    static bool InvalidQualifier(string name) => string.IsNullOrWhiteSpace(name) || name.Any(char.IsControl);
}
