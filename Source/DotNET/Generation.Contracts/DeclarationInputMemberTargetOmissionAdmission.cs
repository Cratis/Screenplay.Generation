// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class DeclarationInputMemberTargetOmissionAdmission
{
    public static void Validate(IReadOnlyList<GenerationFact> facts, string adapter, ISourceAuthorityValidator? authority, AdapterContributionAdmissionContext context)
    {
        var ids = facts.Select(fact => fact.Id.Value).ToHashSet(StringComparer.Ordinal);
        var typeUses = facts.OfType<DeclarationInputMemberTypeUseFact>().ToLookup(fact => fact.Definition.Member);
        var omissions = facts.OfType<DeclarationInputMemberTargetOmittedFact>().ToArray();
        foreach (var omission in omissions)
        {
            var candidates = typeUses[omission.Definition.Member].ToArray();
            if (candidates.Length == 0 || candidates.Any(candidate => !Matches(candidate, omission)))
            {
                Invalid(omission, "Target omission must identify a declaration input type use with the same subject and producer in this contribution", context);
            }

            if (omissions.Count(candidate => candidate.Definition.Member == omission.Definition.Member) > 1)
            {
                Invalid(omission, "Declaration input target omission occurs more than once in this contribution", context);
            }

            AdapterContributionAdmissionValidator.ValidateMethodDiagnostic((adapter, ids, authority), omission.Definition.Diagnostic, context);
        }
    }

    public static void Validate(DeclarationInputMemberTargetOmittedFact fact, string path, AdapterContributionAdmissionContext context)
    {
        DeclarationInputMemberAdmissionValidator.Member(fact.Definition.Member, fact, path, context);
        var diagnostic = fact.Definition.Diagnostic;
        if (diagnostic.Severity != GenerationDiagnosticSeverity.Warning ||
            diagnostic.Subject != fact.Subject || diagnostic.Source != fact.Evidence.Source)
        {
            Invalid(fact, "Declaration input target omission requires a producer-owned warning for the same subject and source", context);
        }
    }

    public static DeclarationInputMemberTargetOmittedFact Freeze(DeclarationInputMemberTargetOmittedFact fact, FactEnvelope envelope, AdapterContributionAdmissionContext context)
    {
        if (fact.Definition?.Diagnostic is null)
        {
            context.Missing($"{envelope.Path}.Definition.Diagnostic");
        }

        return new()
        {
            Id = envelope.Id,
            Subject = envelope.Subject,
            Evidence = envelope.Evidence,
            Definition = new DeclarationInputMemberTargetOmittedDefinition
            {
                Member = DeclarationInputMemberFreezer.Member(fact.Definition?.Member, $"{envelope.Path}.Definition.Member", context),
                Diagnostic = AdapterContributionFreezer.FreezeMethodDiagnostic(
                    fact.Definition?.Diagnostic ?? new GenerationDiagnostic { Code = string.Empty, Severity = GenerationDiagnosticSeverity.Unknown, Message = string.Empty },
                    $"{envelope.Path}.Definition.Diagnostic",
                    context)
            }
        };
    }

    static bool Matches(DeclarationInputMemberTypeUseFact candidate, DeclarationInputMemberTargetOmittedFact omission) =>
        candidate.Definition.Type.ObservedTypeSubject is not null &&
        candidate.Subject == omission.Subject && candidate.Evidence.Adapter == omission.Evidence.Adapter;

    static void Invalid(DeclarationInputMemberTargetOmittedFact fact, string message, AdapterContributionAdmissionContext context) =>
        context.Add(AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch, $"Contribution.Facts[{fact.Id}].Definition", message, fact.Id, fact.Subject);
}
