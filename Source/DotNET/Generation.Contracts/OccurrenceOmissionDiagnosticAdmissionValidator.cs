// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class OccurrenceOmissionDiagnosticAdmissionValidator
{
    public static void ValidateShape(OccurrenceOmissionDiagnosticFact fact, string path, AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        AdapterContributionAdmissionValidator.ValidateArtifactKey(definition.Occurrence, $"{path}.Definition.Occurrence", fact.Id, context);
        context.Enum(definition.Policy, OccurrenceOmissionDiagnosticPolicy.Unknown, $"{path}.Definition.Policy", fact.Id, fact.Subject);
        context.Enum(definition.Preference, OmissionDiagnosticEvidencePreference.Unknown, $"{path}.Definition.Preference", fact.Id, fact.Subject);
        if (!AdapterContributionText.IsNormalized(definition.Family.Value, false) || definition.Occurrence.Subject != fact.Subject)
        {
            Invalid(fact, "An omission candidate requires a normalized family and an occurrence matching its subject", context);
        }
    }

    public static void Validate(IReadOnlyList<GenerationFact> facts, string adapter, ISourceAuthorityValidator? authority, AdapterContributionAdmissionContext context)
    {
        var ids = facts.Select(fact => fact.Id.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var candidate in facts.OfType<OccurrenceOmissionDiagnosticFact>())
        {
            var diagnostic = candidate.Definition.Diagnostic;
            AdapterContributionAdmissionValidator.ValidateMethodDiagnostic((adapter, ids, authority), diagnostic, context);
            if (!MatchingPayload(candidate, diagnostic))
            {
                Invalid(candidate, "An omission payload requires matching located evidence and subject, and nonempty local support excluding itself", context);
            }

            var supports = facts.OfType<MethodFactSupportFact>().Where(support => support.Definition.Candidate == candidate.Id);
            if (supports.SelectMany(support => support.Definition.Alternatives).Any(alternative => alternative.Scope != candidate.Definition.Occurrence.Scope))
            {
                Invalid(candidate, "An omission candidate's method support must match its exact occurrence scope", context);
            }
        }
    }

    static bool MatchingPayload(GenerationFact fact, GenerationDiagnostic diagnostic) =>
        diagnostic.Source is not null && diagnostic.Source == fact.Evidence.Source && diagnostic.Subject == fact.Subject &&
        !diagnostic.Facts.IsEmpty && !diagnostic.Facts.Contains(fact.Id);

    static void Invalid(GenerationFact fact, string message, AdapterContributionAdmissionContext context) =>
        context.Add(AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand, $"Contribution.Facts[{fact.Id}].Definition", message, fact.Id, fact.Subject);
}
