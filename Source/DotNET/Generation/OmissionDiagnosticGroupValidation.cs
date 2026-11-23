// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class OmissionDiagnosticGroupValidation
{
    public static bool Valid(OmittedFactDiagnosticGroupFact group) =>
        group.Definition is { CoveredFacts.Count: > 0, Diagnostic: not null } definition &&
        DiagnosticIntentValidation.Payload(group, definition.Diagnostic) && Coverage(group);

    public static IEnumerable<GenerationDiagnostic> Validate(IReadOnlyList<GenerationFact> facts)
    {
        var groups = facts.OfType<OmittedFactDiagnosticGroupFact>().ToArray();
        var overlaps = groups.Where(Valid).SelectMany(group => group.Definition.CoveredFacts).GroupBy(id => id).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
        return groups.Where(group => !Valid(group) || !Local(group, facts) || group.Definition.CoveredFacts.Any(overlaps.Contains)).Select(Invalid);
    }

    public static GenerationDiagnostic Invalid(OmittedFactDiagnosticGroupFact group) => new()
    {
        Code = GenerationDiagnosticCodes.InvalidOmissionDiagnosticGroup, Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = GenerationDiagnosticOutcome.Unsupported, Message = "Omission diagnostic group metadata or local coverage is invalid",
        Facts = [group.Id], Subject = group.Subject, Source = group.Evidence.Source
    };

    static bool Coverage(OmittedFactDiagnosticGroupFact group) =>
        group.Definition.CoveredFacts.Distinct().Count() == group.Definition.CoveredFacts.Count &&
        group.Definition.CoveredFacts.All(id => id is not null && id != group.Id && DiagnosticIntentValidation.Text(id.Value) && id.Value.StartsWith($"{group.Evidence.Adapter.Id}:", StringComparison.Ordinal));

    static bool Local(OmittedFactDiagnosticGroupFact group, IReadOnlyList<GenerationFact> facts) =>
        group.Definition.CoveredFacts.Concat(group.Definition.Diagnostic.Facts).Append(group.Id).All(id =>
            facts.Count(fact => fact.Id == id && fact.Evidence.Adapter == group.Evidence.Adapter) == 1);
}
