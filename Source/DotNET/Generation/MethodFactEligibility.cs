// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class MethodFactEligibility
{
    public static EffectiveGenerationInputs Select(ImmutableArray<GenerationFact> facts, HashSet<MethodFactSupportFact>? invalidSupports = null) =>
        new Selection(facts, invalidSupports ?? []).Select();

    public static bool HasCandidate(MethodFactSupportFact support, IEnumerable<GenerationFact> facts)
    {
        var candidates = facts.Where(fact => fact.Id == support.Definition?.Candidate && fact.Evidence.Adapter == support.Evidence.Adapter).ToArray();
        return candidates.Length == 1 && IsCandidateOf(candidates[0], support);
    }

    static bool IsCandidateOf(GenerationFact fact, MethodFactSupportFact support) =>
        fact is not (MethodExecutionClassificationFact or MethodFactSupportFact) && GenerationFactInstance.Of(fact) == GenerationFactInstance.CandidateOf(support);

    static bool ValidClassification(MethodExecutionClassificationFact fact)
    {
        var definition = fact.Definition;
        if (!ValidClassificationIdentity(fact))
        {
            return false;
        }

        return definition.Kind switch
        {
            MethodExecutionKind.Ordinary => definition.OmissionReason is null,
            MethodExecutionKind.FailureOnly => definition.OmissionReason is { Severity: GenerationDiagnosticSeverity.Warning, Source: not null } reason && ValidDiagnostic(reason),
            _ => false
        };
    }

    static bool ValidClassificationIdentity(MethodExecutionClassificationFact fact) =>
        ValidSubject(fact.Definition?.Method) && fact.Definition!.Method == fact.Subject &&
        fact.Evidence.Strength == EvidenceStrength.Exact && ValidScope(fact.Definition.Scope);

    static bool ValidAlternatives(IReadOnlyList<MethodFactSupportAlternative>? alternatives) =>
        alternatives is { Count: > 0 } && alternatives.All(alternative =>
            alternative is not null && ValidSubject(alternative.Method) &&
            alternative.EnclosingExecutables?.All(ValidSubject) == true &&
            alternative.Diagnostics?.All(ValidDiagnostic) == true && ValidScope(alternative.Scope));

    static bool ValidSubject(SubjectId? subject) => !string.IsNullOrWhiteSpace(subject?.Value);

    static bool ValidScope(BackendScopeKey? scope) => scope is null || (ValidSubject(scope.Store) && (scope.EventModelName is null || !string.IsNullOrWhiteSpace(scope.EventModelName)));

    static bool ValidDiagnostic(GenerationDiagnostic? diagnostic) => diagnostic is not null &&
        !string.IsNullOrWhiteSpace(diagnostic.Code) && !string.IsNullOrWhiteSpace(diagnostic.Message) &&
        Enum.IsDefined(diagnostic.Severity) && diagnostic.Severity != GenerationDiagnosticSeverity.Unknown &&
        !diagnostic.Facts.IsDefault && diagnostic.Facts.All(id => !string.IsNullOrWhiteSpace(id?.Value));

    static IEnumerable<SubjectId> Executables(MethodFactSupportAlternative alternative) => alternative.EnclosingExecutables.Append(alternative.Method).Distinct();

    static string Key(SubjectId? method, BackendScopeKey? scope) => Structural.WithScopes(method?.Value ?? string.Empty, scope);

    static GenerationDiagnostic Diagnostic(IEnumerable<GenerationFact> facts, GenerationDiagnosticOutcome outcome)
    {
        var ordered = facts.OrderBy(fact => fact.Id.Value, StringComparer.Ordinal).ThenBy(fact => Structural.Evidence(fact.Evidence), StringComparer.Ordinal).ToArray();
        return new()
        {
            Code = outcome == GenerationDiagnosticOutcome.Conflict ? GenerationDiagnosticCodes.ConflictingMethodExecutionClassification : GenerationDiagnosticCodes.InvalidMethodFactEligibility,
            Severity = GenerationDiagnosticSeverity.Warning,
            Message = outcome == GenerationDiagnosticOutcome.Conflict ? "Exact method execution classifications conflict; dependent behavior was omitted" : "Invalid method eligibility metadata; dependent behavior was omitted",
            Outcome = outcome,
            Facts = [.. ordered.Select(fact => fact.Id).Distinct()],
            Source = LoweringSupport.FirstSource(ordered.Select(fact => fact.Evidence))
        };
    }

    sealed record Classification(MethodExecutionKind Kind, bool Conflicted, ImmutableArray<MethodExecutionClassificationFact> Facts);

    sealed class Selection(ImmutableArray<GenerationFact> facts, HashSet<MethodFactSupportFact> invalidSupports)
    {
        readonly List<GenerationDiagnostic> _diagnostics = [];
        readonly Dictionary<string, Classification> _classifications = [];
        readonly ImmutableDictionary<GenerationFactInstance, GenerationFactDisposition>.Builder _dispositions = ImmutableDictionary.CreateBuilder<GenerationFactInstance, GenerationFactDisposition>();
        readonly ImmutableDictionary<FactId, ImmutableArray<GenerationFact>>.Builder _inputs = ImmutableDictionary.CreateBuilder<FactId, ImmutableArray<GenerationFact>>();
        readonly ImmutableDictionary<GenerationFactInstance, ImmutableArray<GenerationDiagnostic>>.Builder _selectionDiagnostics = ImmutableDictionary.CreateBuilder<GenerationFactInstance, ImmutableArray<GenerationDiagnostic>>();
        readonly ImmutableDictionary<GenerationFactInstance, ImmutableArray<GenerationFact>>.Builder _selectionInputs = ImmutableDictionary.CreateBuilder<GenerationFactInstance, ImmutableArray<GenerationFact>>();
        readonly ILookup<FactId, GenerationFact> _candidates = facts.ToLookup(fact => fact.Id);
        readonly HashSet<BackendScopeKey> _scopes = [.. facts.OfType<BackendScopeFact>().Select(fact => fact.Definition.Key)];

        public EffectiveGenerationInputs Select()
        {
            Classify();
            foreach (var group in facts.OfType<MethodFactSupportFact>().GroupBy(GenerationFactInstance.CandidateOf))
            {
                SelectCandidate([.. group]);
            }

            var effective = facts.Where(fact => !_dispositions.ContainsKey(GenerationFactInstance.Of(fact))).ToImmutableArray();
            return new(facts, effective, [.. _diagnostics.GroupBy(Canonical.Diagnostic, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal).Select(group => group.First())], _dispositions.ToImmutable(), _inputs.ToImmutable())
            {
                RoleSelection = new() { Diagnostics = _selectionDiagnostics.ToImmutable(), Inputs = _selectionInputs.ToImmutable() }
            };
        }

        static GenerationFactDisposition ClassificationDisposition(Classification classification)
        {
            if (classification.Conflicted)
            {
                return GenerationFactDisposition.Conflicted;
            }

            return classification.Kind == MethodExecutionKind.Unknown ? GenerationFactDisposition.OmittedWithDiagnostic : GenerationFactDisposition.ProvenanceOnly;
        }

        void Classify()
        {
            var occurrences = facts.OfType<MethodExecutionClassificationFact>().SelectMany(fact => new[]
            {
                (Key: Key(fact.Definition?.Method, fact.Definition?.Scope), Fact: fact),
                (Key: Key(fact.Subject, fact.Definition?.Scope), Fact: fact)
            }.DistinctBy(occurrence => occurrence.Key));
            foreach (var group in occurrences.GroupBy(occurrence => occurrence.Key))
            {
                var assertions = group.Select(occurrence => occurrence.Fact).OrderBy(fact => fact.Id.Value, StringComparer.Ordinal).ThenBy(fact => Structural.Evidence(fact.Evidence), StringComparer.Ordinal).ToImmutableArray();
                var classification = ClassifyAssertions(assertions);
                _classifications.Add(group.Key, classification);
                var disposition = ClassificationDisposition(classification);
                foreach (var fact in assertions)
                {
                    _dispositions[GenerationFactInstance.Of(fact)] = disposition;
                }
            }
        }

        void SelectCandidate(MethodFactSupportFact[] supports)
        {
            var support = supports[0];
            var candidate = support.Definition?.Candidate;
            foreach (var fact in supports)
            {
                _dispositions[GenerationFactInstance.Of(fact)] = GenerationFactDisposition.ProvenanceOnly;
            }

            if (!ValidSupport(supports))
            {
                Invalid(supports);
                return;
            }

            var alternatives = support.Definition!.Alternatives.Select(alternative => new
            {
                Alternative = alternative,
                Classifications = Executables(alternative).Select(method => _classifications.GetValueOrDefault(Key(method, alternative.Scope))).OfType<Classification>().ToArray()
            }).ToArray();
            var eligible = alternatives.Where(alternative => alternative.Classifications.All(classification => classification.Kind == MethodExecutionKind.Ordinary)).ToArray();
            _diagnostics.AddRange(alternatives.SelectMany(alternative => alternative.Classifications).SelectMany(classification => classification.Facts).Where(ValidClassification).Select(fact => fact.Definition.OmissionReason).OfType<GenerationDiagnostic>());
            if (eligible.Length == 0)
            {
                var conflicted = alternatives.SelectMany(alternative => alternative.Classifications).Any(classification => classification.Conflicted);
                _dispositions[GenerationFactInstance.CandidateOf(support)] = conflicted ? GenerationFactDisposition.Conflicted : GenerationFactDisposition.OmittedWithDiagnostic;
                _dispositions[GenerationFactInstance.Of(support)] = conflicted ? GenerationFactDisposition.Conflicted : GenerationFactDisposition.ProvenanceOnly;
                return;
            }

            ImmutableArray<GenerationFact> dependencies = [support, .. eligible.SelectMany(alternative => alternative.Classifications).SelectMany(classification => classification.Facts)];
            if (_candidates[candidate!].OfType<ArtifactMemberRoleSelectionFact>().Count(fact => IsCandidateOf(fact, support)) == 1)
            {
                _selectionDiagnostics[GenerationFactInstance.CandidateOf(support)] = [.. eligible.SelectMany(alternative => alternative.Alternative.Diagnostics)];
                _selectionInputs[GenerationFactInstance.CandidateOf(support)] = dependencies;
            }
            else
            {
                _diagnostics.AddRange(eligible.SelectMany(alternative => alternative.Alternative.Diagnostics));
                _inputs[candidate!] = dependencies;
            }
        }

        Classification ClassifyAssertions(ImmutableArray<MethodExecutionClassificationFact> assertions)
        {
            var conflict = assertions.Select(fact => fact.Definition?.Kind).Distinct().Count() > 1;
            var invalid = assertions.Any(fact => !ValidClassification(fact) || !KnownScope(fact.Definition?.Scope));
            if (invalid || conflict)
            {
                _diagnostics.Add(Diagnostic(assertions, conflict ? GenerationDiagnosticOutcome.Conflict : GenerationDiagnosticOutcome.Unknown));
                return new(MethodExecutionKind.Unknown, conflict, assertions);
            }

            return new(assertions[0].Definition.Kind, false, assertions);
        }

        bool ValidSupport(MethodFactSupportFact[] supports)
        {
            if (supports.Length != 1 || invalidSupports.Contains(supports[0]))
            {
                return false;
            }

            var support = supports[0];
            var definition = support.Definition;
            return definition?.Candidate is { } candidate && HasCandidate(support, _candidates[candidate]) &&
                ValidAlternatives(definition.Alternatives) && definition.Alternatives.All(alternative => KnownScope(alternative.Scope));
        }

        void Invalid(MethodFactSupportFact[] supports)
        {
            _diagnostics.Add(Diagnostic(supports, GenerationDiagnosticOutcome.Unknown));
            foreach (var support in supports)
            {
                _dispositions[GenerationFactInstance.Of(support)] = GenerationFactDisposition.OmittedWithDiagnostic;
                if (support.Definition?.Candidate is { } candidate && _candidates[candidate].Any(fact => IsCandidateOf(fact, support)))
                {
                    _dispositions[GenerationFactInstance.CandidateOf(support)] = GenerationFactDisposition.OmittedWithDiagnostic;
                }
            }
        }

        bool KnownScope(BackendScopeKey? scope) => scope is null || _scopes.Contains(scope);
    }
}
