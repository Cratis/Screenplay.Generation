// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class OccurrenceOmissionDiagnosticValidation
{
    public static bool Valid(OccurrenceOmissionDiagnosticFact fact) => fact.Definition is { Occurrence: not null, Family: not null } definition &&
        DiagnosticIntentValidation.Text(definition.Family.Value) && ValidOccurrence(fact, definition.Occurrence) &&
        definition.Policy == OccurrenceOmissionDiagnosticPolicy.OnePerOccurrence && Rank(definition.Preference) > 0 &&
        DiagnosticIntentValidation.LocatedPayload(fact, definition.Diagnostic);

    public static int Rank(OmissionDiagnosticEvidencePreference preference) => preference switch
    {
        OmissionDiagnosticEvidencePreference.IdentityConfiguration => 3,
        OmissionDiagnosticEvidencePreference.ExplicitRegistration => 2,
        OmissionDiagnosticEvidencePreference.UseSite => 1,
        _ => 0
    };

    public static GenerationDiagnostic Invalid(GenerationFact fact) => new()
    {
        Code = GenerationDiagnosticCodes.InvalidOccurrenceOmissionDiagnostic, Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = GenerationDiagnosticOutcome.Unknown, Message = "Occurrence omission diagnostic metadata or local support is invalid; no adapter payload was emitted",
        Facts = [fact.Id], Source = fact.Evidence.Source, Subject = fact.Subject
    };

    public static bool SupportsMatch(OccurrenceOmissionDiagnosticFact candidate, IEnumerable<GenerationFact> facts)
    {
        var supports = facts.OfType<MethodFactSupportFact>().Where(support => support.Definition?.Candidate == candidate.Id && support.Evidence.Adapter == candidate.Evidence.Adapter).ToArray();
        return supports.Length <= 1 && supports.All(support => support.Definition.Alternatives is { Count: > 0 } alternatives &&
            alternatives.All(alternative => alternative is not null && alternative.Scope == candidate.Definition.Occurrence.Scope));
    }

    static bool ValidOccurrence(GenerationFact fact, ArtifactKey key) =>
        DiagnosticIntentValidation.Subject(key.Subject) && key.Subject == fact.Subject && key.Kind != ArtifactKind.Unknown && Enum.IsDefined(key.Kind) &&
        DiagnosticIntentValidation.Scope(key.Scope);
}
