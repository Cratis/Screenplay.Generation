// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class MethodEligibilityAdmissionValidator
{
    public static void Validate(IReadOnlyList<GenerationFact> facts, string adapter, ISourceAuthorityValidator? authority, AdapterContributionAdmissionContext context)
    {
        var ids = facts.Select(fact => fact.Id.Value).ToHashSet(StringComparer.Ordinal);
        var candidates = facts.ToLookup(fact => fact.Id);
        var supports = facts.OfType<MethodFactSupportFact>().ToLookup(fact => fact.Definition.Candidate);
        foreach (var fact in facts)
        {
            switch (fact)
            {
                case MethodExecutionClassificationFact classification:
                    Classification(classification, context);
                    if (classification.Definition.OmissionReason is { } reason)
                    {
                        AdapterContributionAdmissionValidator.ValidateMethodDiagnostic((adapter, ids, authority), reason, context);
                    }

                    break;
                case MethodFactSupportFact support:
                    Support(support, [.. candidates[support.Definition.Candidate]], supports[support.Definition.Candidate].Count(), context);
                    foreach (var diagnostic in support.Definition.Alternatives.SelectMany(alternative => alternative.Diagnostics))
                    {
                        AdapterContributionAdmissionValidator.ValidateMethodDiagnostic((adapter, ids, authority), diagnostic, context);
                    }

                    break;
            }
        }
    }

    static void Classification(MethodExecutionClassificationFact fact, AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        var path = $"Contribution.Facts[{fact.Id}].Definition";
        Subject(definition.Method, $"{path}.Method", fact, context);
        BackendScopeFactAdmissionValidator.ValidateKey(definition.Scope, $"{path}.Scope", (fact.Id, fact.Subject), context);
        context.Enum(definition.Kind, MethodExecutionKind.Unknown, $"{path}.Kind", fact.Id, fact.Subject);
        if (definition.Method != fact.Subject)
        {
            Invalid(fact, path, "Classification subject must equal the exact method", context);
        }

        if (fact.Evidence.Strength != EvidenceStrength.Exact)
        {
            Invalid(fact, path, "Execution classification requires exact evidence", context);
        }

        var reason = definition.OmissionReason;
        if (definition.Kind == MethodExecutionKind.FailureOnly && !IsLocatedWarning(reason))
        {
            Invalid(fact, path, "Failure-only classification requires the producer's located warning", context);
        }

        if (definition.Kind == MethodExecutionKind.Ordinary && reason is not null)
        {
            Invalid(fact, path, "Ordinary classification cannot carry an omission reason", context);
        }
    }

    static void Support(MethodFactSupportFact fact, GenerationFact[] candidates, int count, AdapterContributionAdmissionContext context)
    {
        var path = $"Contribution.Facts[{fact.Id}].Definition";
        Candidate(fact, candidates, path, context);

        if (count != 1 || fact.Definition.Alternatives.Count == 0)
        {
            Invalid(fact, path, "A candidate requires one support facet with nonempty alternatives", context);
        }

        foreach (var alternative in fact.Definition.Alternatives)
        {
            Subject(alternative.Method, $"{path}.Alternatives.Method", fact, context);
            foreach (var enclosing in alternative.EnclosingExecutables)
            {
                Subject(enclosing, $"{path}.Alternatives.EnclosingExecutables", fact, context);
            }

            BackendScopeFactAdmissionValidator.ValidateKey(alternative.Scope, $"{path}.Alternatives.Scope", (fact.Id, fact.Subject), context);
        }
    }

    static bool IsLocatedWarning(GenerationDiagnostic? reason) => reason?.Source is not null && reason.Severity == GenerationDiagnosticSeverity.Warning;

    static void Candidate(MethodFactSupportFact fact, GenerationFact[] candidates, string path, AdapterContributionAdmissionContext context)
    {
        if (candidates.Length != 1 || candidates[0] is MethodFactSupportFact or MethodExecutionClassificationFact)
        {
            Invalid(fact, path, "Support must target one same-contribution semantic candidate", context);
        }
        else if (candidates[0].Subject != fact.Subject || candidates[0].Evidence.Adapter != fact.Evidence.Adapter)
        {
            Invalid(fact, path, "Support must share its candidate's subject and producer", context);
        }
    }

    static void Subject(SubjectId subject, string path, GenerationFact fact, AdapterContributionAdmissionContext context) =>
        AdapterContributionAdmissionValidator.ValidateSubject(subject, path, fact.Id, context);

    static void Invalid(GenerationFact fact, string path, string message, AdapterContributionAdmissionContext context) =>
        context.Add(AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch, path, message, fact.Id, fact.Subject);
}
