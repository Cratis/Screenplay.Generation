// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class ArtifactMemberRoleSelection
{
    public static readonly GenerationDerivationRuleIdentity Rule = new() { Id = "cratis.screenplay.artifact-member-role-selection", Version = "1.0.0" };

    public static EffectiveGenerationInputs Select(EffectiveGenerationInputs input) => new Selection(input).Run();

    sealed class Selection(EffectiveGenerationInputs input)
    {
        readonly List<GenerationDiagnostic> _diagnostics = [.. input.Diagnostics];
        readonly List<GenerationFactRecord> _roles = [];
        readonly List<GenerationDerivationRuleRecord> _rules = [];
        readonly ImmutableDictionary<GenerationFactInstance, GenerationFactDisposition>.Builder _dispositions = input.Dispositions.ToBuilder();

        public EffectiveGenerationInputs Run()
        {
            var candidates = ArtifactMemberRoleSelectionSupport.Candidates(input).DistinctBy(Structural.Fact).ToArray();
            foreach (var invalid in candidates.Where(fact => !ArtifactMemberRoleSelectionValidation.Key(fact)))
            {
                Complete([invalid], GenerationDiagnosticCodes.InvalidArtifactMemberRoleSelection, GenerationDiagnosticOutcome.Unknown, null);
            }

            foreach (var group in candidates.Where(ArtifactMemberRoleSelectionValidation.Key).GroupBy(fact => ArtifactMemberRoleSelectionStructure.Group(fact.Definition)).OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                SelectGroup([.. group.OrderBy(Structural.Fact, StringComparer.Ordinal)]);
            }

            return input with
            {
                Facts = [.. input.Facts.Where(fact => fact is not ArtifactMemberRoleSelectionFact)],
                Diagnostics = [.. _diagnostics.DistinctBy(Canonical.Diagnostic).OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)],
                Dispositions = _dispositions.ToImmutable(), SelectedRoles = [.. _roles], RoleSelectionRules = [.. _rules]
            };
        }

        void SelectGroup(ArtifactMemberRoleSelectionFact[] candidates)
        {
            if (candidates.Any(candidate => !ArtifactMemberRoleSelectionValidation.Valid(candidate, input)))
            {
                Complete(candidates, GenerationDiagnosticCodes.InvalidArtifactMemberRoleSelection, GenerationDiagnosticOutcome.Unknown, null);
                return;
            }

            if (ArtifactMemberRoleSelectionMixing.Assertions(candidates[0], input.Facts).Count > 0)
            {
                Complete(candidates, GenerationDiagnosticCodes.ConflictingArtifactMemberRoleSelection, GenerationDiagnosticOutcome.Conflict, null);
                return;
            }

            var ranked = candidates.OrderByDescending(candidate => candidate.Definition.Priority).ThenByDescending(candidate => candidate.Definition.ObservationOrder).ToArray();
            var winner = ranked[0];
            var ambiguous = candidates.GroupBy(candidate => (candidate.Definition.Priority, candidate.Definition.ObservationOrder)).Any(group => group.Count() > 1);
            Complete(candidates, ambiguous ? GenerationDiagnosticCodes.ConflictingArtifactMemberRoleSelection : null, GenerationDiagnosticOutcome.Conflict, ambiguous ? null : winner);
        }

        void Complete(ArtifactMemberRoleSelectionFact[] candidates, string? code, GenerationDiagnosticOutcome outcome, ArtifactMemberRoleSelectionFact? winner)
        {
            var lineage = ArtifactMemberRoleSelectionLineage.Create(candidates, input);
            var diagnostics = code is null ? WinnerDiagnostics(winner!, lineage) : [Invalid(candidates, code, outcome, lineage)];
            var disposition = code is null ? GenerationFactDisposition.ProvenanceOnly : FailureDisposition(outcome);
            var role = winner?.Definition.Member is { } member ? Role(winner, member, lineage) : null;
            foreach (var candidate in candidates)
            {
                _dispositions[GenerationFactInstance.Of(candidate)] = disposition;
            }

            _diagnostics.AddRange(diagnostics);
            if (role is not null)
            {
                _roles.Add(role);
            }

            _rules.Add(new() { Rule = Rule, Inputs = lineage.Inputs, Outputs = role is null ? [] : [role.Fact.Id], Diagnostics = [.. diagnostics] });
        }

        GenerationFactDisposition FailureDisposition(GenerationDiagnosticOutcome outcome) => outcome == GenerationDiagnosticOutcome.Conflict ? GenerationFactDisposition.Conflicted : GenerationFactDisposition.OmittedWithDiagnostic;

        GenerationDiagnostic[] WinnerDiagnostics(ArtifactMemberRoleSelectionFact winner, GenerationFactLineage lineage) =>
        [
            .. winner.Definition.Diagnostics.Concat(input.SelectionDiagnostics.GetValueOrDefault(GenerationFactInstance.Of(winner), []))
                .Select(diagnostic => diagnostic with { Facts = [.. diagnostic.Facts.Concat(lineage.Inputs).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)], Lineage = lineage })
                .DistinctBy(Canonical.Diagnostic).OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)
        ];

        GenerationFactRecord Role(ArtifactMemberRoleSelectionFact winner, ArtifactMemberKey member, GenerationFactLineage lineage) => new()
        {
            Fact = new ArtifactMemberRoleFact
            {
                Id = new() { Value = $"generation:derive:member-role-selection:1:{Uri.EscapeDataString(Structural.Fact(winner))}" }, Subject = winner.Subject,
                Evidence = winner.Evidence,
                Definition = new() { Member = member, Role = winner.Definition.Role }
            },
            Lineage = lineage
        };

        GenerationDiagnostic Invalid(ArtifactMemberRoleSelectionFact[] candidates, string code, GenerationDiagnosticOutcome outcome, GenerationFactLineage lineage) => new()
        {
            Code = code, Severity = GenerationDiagnosticSeverity.Error, Outcome = outcome,
            Message = "The exact artifact member role has invalid or ambiguous selection alternatives; no member role was selected",
            Subject = candidates[0].Subject, Source = LoweringSupport.FirstSource(candidates.Select(candidate => candidate.Evidence)), Facts = lineage.Inputs, Lineage = lineage
        };
    }
}
