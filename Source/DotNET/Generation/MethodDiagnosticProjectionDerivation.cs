// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class MethodDiagnosticProjectionDerivation
{
    public static GenerationDerivationRuleIdentity Rule { get; } = new() { Id = "cratis.screenplay.method-diagnostic-projection", Version = "1.0.0" };

    public static ImmutableArray<GenerationDerivationRuleRecord> Derive(ImmutableArray<GenerationFactRecord> records, EffectiveGenerationInputs effective)
    {
        var facts = records.Select(record => record.Fact).ToArray();
        var intents = facts.OfType<MethodDiagnosticProjectionFact>().ToArray();
        if (intents.Length == 0)
        {
            return [];
        }

        var diagnostics = intents.Select(intent => Project(intent, facts, effective)).ToArray();
        return
        [
            new()
            {
                Rule = Rule,
                Inputs = [.. diagnostics.SelectMany(diagnostic => diagnostic.Lineage?.Inputs ?? diagnostic.Facts).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)],
                Outputs = [], Diagnostics = [.. diagnostics]
            }
        ];
    }

    static GenerationDiagnostic Project(MethodDiagnosticProjectionFact intent, GenerationFact[] facts, EffectiveGenerationInputs effective)
    {
        var definition = intent.Definition;
        if (!ValidSupport(intent, [.. effective.OriginalFacts]) || !DiagnosticIntentLineage.LocalSupportsExist(intent, definition.Diagnostic, facts))
        {
            return DiagnosticIntentValidation.Invalid(intent);
        }

        var entries = Entries(intent, facts);
        var keys = entries.Select(entry => entry.Definition.Artifact).Distinct().ToArray();
        var inputs = DiagnosticIntentLineage.LocalInputs(intent, definition.Diagnostic, facts).Concat(entries)
            .Concat(facts.OfType<BackendScopeFact>().Where(scope => scope.Definition.Key == definition.Scope));
        var lineage = DiagnosticIntentLineage.Create(Rule, inputs, effective);
        return keys.Length == 1
            ? definition.Diagnostic with { Subject = keys[0].Subject, Lineage = lineage }
            : new()
            {
                Code = keys.Length == 0 ? GenerationDiagnosticCodes.MissingDiagnosticEntryPoint : GenerationDiagnosticCodes.AmbiguousDiagnosticEntryPoint,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = keys.Length == 0 ? GenerationDiagnosticOutcome.Unknown : GenerationDiagnosticOutcome.Conflict,
                Message = keys.Length == 0 ? "No exact compatible entry point exists for the method diagnostic" : "Several distinct exact compatible entry points exist for the method diagnostic",
                Facts = [intent.Id], Source = intent.Evidence.Source, Subject = intent.Subject, Lineage = lineage
            };
    }

    static EntryPointFact[] Entries(MethodDiagnosticProjectionFact intent, GenerationFact[] facts) =>
        [.. facts.OfType<EntryPointFact>().Where(entry => entry.Evidence.Strength == EvidenceStrength.Exact &&
            entry.Definition.Method == intent.Definition.Method && entry.Definition.Artifact.Kind == intent.Definition.EntryPointRole && entry.Definition.Artifact.Scope == intent.Definition.Scope)];

    static bool ValidSupport(MethodDiagnosticProjectionFact intent, GenerationFact[] facts)
    {
        var supports = facts.OfType<MethodFactSupportFact>().Where(support => support.Definition?.Candidate == intent.Id && support.Evidence.Adapter == intent.Evidence.Adapter).ToArray();
        return supports.Length == 1 && supports[0].Definition.Alternatives.All(alternative => alternative.Method == intent.Definition.Method && alternative.Scope == intent.Definition.Scope);
    }
}
