// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class DiagnosticIntentValidation
{
    public static bool Valid(MethodDiagnosticProjectionFact fact) => fact.Definition is { } definition &&
        definition.Mode == MethodDiagnosticProjectionMode.EntryPointArtifact &&
        definition.EntryPointRole is ArtifactKind.Command or ArtifactKind.Query &&
        Subject(definition.Method) && definition.Method == fact.Subject && Scope(definition.Scope) &&
        Payload(fact, definition.Diagnostic);

    public static bool Payload(GenerationFact fact, GenerationDiagnostic? diagnostic) =>
        diagnostic is { Source: not null, Lineage: null } && Metadata(diagnostic) && Located(fact, diagnostic) && LocalReferences(fact, diagnostic);

    public static bool Subject(SubjectId? subject) => Text(subject?.Value) && Uri.TryCreate(subject!.Value, UriKind.Absolute, out _);

    public static bool Scope(BackendScopeKey? scope) => scope is null || (Subject(scope.Store) && (scope.EventModelName is null || Text(scope.EventModelName)));

    public static bool Text(string? value) => !string.IsNullOrWhiteSpace(value) && value == value.Trim() && !value.Any(char.IsControl);

    public static GenerationDiagnostic Invalid(GenerationFact fact) => new()
    {
        Code = GenerationDiagnosticCodes.InvalidDiagnosticIntent,
        Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = GenerationDiagnosticOutcome.Unknown,
        Message = "Diagnostic intent metadata is invalid; no adapter payload was emitted",
        Facts = [fact.Id], Source = fact.Evidence.Source, Subject = fact.Subject
    };

    static bool Metadata(GenerationDiagnostic diagnostic) => Text(diagnostic.Code) && Text(diagnostic.Message) &&
        diagnostic.Severity != GenerationDiagnosticSeverity.Unknown && Enum.IsDefined(diagnostic.Severity) &&
        (diagnostic.Outcome is null || Enum.IsDefined(diagnostic.Outcome.Value));

    static bool Located(GenerationFact fact, GenerationDiagnostic diagnostic) => diagnostic.Subject == fact.Subject &&
        fact.Evidence.Strength == EvidenceStrength.Exact && diagnostic.Source == fact.Evidence.Source;

    static bool LocalReferences(GenerationFact fact, GenerationDiagnostic diagnostic) =>
        !diagnostic.Facts.IsDefaultOrEmpty && !diagnostic.Facts.Contains(fact.Id) &&
        diagnostic.Facts.All(id => id is not null && Text(id.Value) && id.Value.StartsWith($"{fact.Evidence.Adapter.Id}:", StringComparison.Ordinal));
}
