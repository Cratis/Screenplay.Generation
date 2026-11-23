// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class OmissionDiagnosticGroupAdmissionValidator
{
    public static void Validate(IReadOnlyList<GenerationFact> facts, AdapterIdentity adapter, ISourceAuthorityValidator? authority, AdapterContributionAdmissionContext context)
    {
        var groups = facts.OfType<OmittedFactDiagnosticGroupFact>().ToArray();
        var ids = facts.Select(fact => fact.Id.Value).ToHashSet(StringComparer.Ordinal);
        var overlaps = groups.SelectMany(group => group.Definition.CoveredFacts).GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
        foreach (var group in groups)
        {
            DiagnosticIntentAdmissionValidator.ValidatePayload(group, group.Definition.Diagnostic, (adapter.Id, ids, authority), context);
            var covered = group.Definition.CoveredFacts;
            if (covered.Count == 0 || covered.Any(id => !Local(id, group, ids) || overlaps.Contains(id)))
            {
                context.Add(AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand, "Contribution.Facts", "Omission groups require disjoint, nonempty local coverage without self references", group.Id, group.Subject);
            }
        }
    }

    static bool Local(FactId id, OmittedFactDiagnosticGroupFact group, HashSet<string> ids) =>
        !string.IsNullOrWhiteSpace(id.Value) && id != group.Id && ids.Contains(id.Value) && id.Value.StartsWith($"{group.Evidence.Adapter.Id}:", StringComparison.Ordinal);
}
