// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class DiagnosticIntentAdmissionValidator
{
    public static void ValidateShape(GenerationFact fact, string path, AdapterContributionAdmissionContext context)
    {
        if (fact is not MethodDiagnosticProjectionFact projection)
        {
            return;
        }

        var definition = projection.Definition;
        AdapterContributionAdmissionValidator.ValidateSubject(definition.Method, $"{path}.Definition.Method", fact.Id, context);
        BackendScopeFactAdmissionValidator.ValidateKey(definition.Scope, $"{path}.Definition.Scope", (fact.Id, fact.Subject), context);
        context.Enum(definition.Mode, MethodDiagnosticProjectionMode.Unknown, $"{path}.Definition.Mode", fact.Id, fact.Subject);
        if (definition.EntryPointRole is not (ArtifactKind.Command or ArtifactKind.Query) || definition.Method != fact.Subject)
        {
            Invalid(fact, path, "Projection requires an exact method subject and a Command or Query entry-point role", context);
        }
    }

    public static void Validate(IReadOnlyList<GenerationFact> facts, string adapter, ISourceAuthorityValidator? authority, AdapterContributionAdmissionContext context)
    {
        var ids = facts.Select(fact => fact.Id.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var projection in facts.OfType<MethodDiagnosticProjectionFact>())
        {
            var diagnostic = projection.Definition.Diagnostic;
            var path = $"Contribution.Facts[{projection.Id}].Definition";
            ValidatePayload(projection, diagnostic, (adapter, ids, authority), context);
            var supports = facts.OfType<MethodFactSupportFact>().Where(support => support.Definition.Candidate == projection.Id).ToArray();
            if (!MatchingSupport(projection, supports))
            {
                Invalid(projection, path, "Projection requires one local eligibility support facet for its exact method and scope", context);
            }

            if (facts.OfType<MethodFactSupportFact>().SelectMany(support => support.Definition.Alternatives).SelectMany(alternative => alternative.Diagnostics).Any(ordinary => SamePayload(ordinary, diagnostic)))
            {
                Invalid(projection, path, "A projection payload must not also be an ordinary alternative diagnostic", context);
            }
        }
    }

    public static void ValidateOrdinary(IReadOnlyList<GenerationFact> facts, IReadOnlyList<GenerationDiagnostic> diagnostics, AdapterContributionAdmissionContext context)
    {
        foreach (var projection in facts.OfType<MethodDiagnosticProjectionFact>().Where(projection => diagnostics.Any(ordinary => SamePayload(ordinary, projection.Definition.Diagnostic))))
        {
            Invalid(projection, "Contribution.Diagnostics", "A projection payload must not also be an ordinary contribution diagnostic", context);
        }
    }

    static bool MatchingSupport(MethodDiagnosticProjectionFact projection, MethodFactSupportFact[] supports) =>
        supports.Length == 1 && supports[0].Definition.Alternatives.All(alternative => alternative.Method == projection.Definition.Method && alternative.Scope == projection.Definition.Scope);

    static void ValidatePayload(GenerationFact fact, GenerationDiagnostic diagnostic, (string AdapterId, HashSet<string> FactIds, ISourceAuthorityValidator? Authority) owner, AdapterContributionAdmissionContext context)
    {
        AdapterContributionAdmissionValidator.ValidateMethodDiagnostic(owner, diagnostic, context);
        if (!MatchingPayload(fact, diagnostic))
        {
            Invalid(fact, $"Contribution.Facts[{fact.Id}].Definition.Diagnostic", "Diagnostic intents require exact matching located evidence, matching subjects, and nonempty local support excluding themselves", context);
        }
    }

    static bool MatchingPayload(GenerationFact fact, GenerationDiagnostic diagnostic) =>
        fact.Evidence.Strength == EvidenceStrength.Exact && diagnostic.Source is not null && diagnostic.Source == fact.Evidence.Source &&
        diagnostic.Subject == fact.Subject && !diagnostic.Facts.IsEmpty && !diagnostic.Facts.Contains(fact.Id);

    static bool SamePayload(GenerationDiagnostic first, GenerationDiagnostic second) =>
        first.Code == second.Code && first.Severity == second.Severity && first.Outcome == second.Outcome &&
        first.Message == second.Message && first.Source == second.Source && first.Subject == second.Subject &&
        first.Facts.OrderBy(id => id.Value, StringComparer.Ordinal).SequenceEqual(second.Facts.OrderBy(id => id.Value, StringComparer.Ordinal));

    static void Invalid(GenerationFact fact, string path, string message, AdapterContributionAdmissionContext context) =>
        context.Add(AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand, path, message, fact.Id, fact.Subject);
}
