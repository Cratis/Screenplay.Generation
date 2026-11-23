// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;

namespace Cratis.Screenplay.Generation;

static class SourcePlacementCandidateDerivation
{
    public static GenerationDerivationRuleIdentity Rule { get; } = new() { Id = "cratis.screenplay.source-owner-placement", Version = "1.0.0" };

    public static ArtifactUsageDerivationResult Derive(ImmutableArray<GenerationFactRecord> records, IArtifactSourcePlacementResolver? resolver) =>
        new Selection([.. records.Select(record => record.Fact)], resolver).Derive();

    public static GenerationFact[] CanonicalInputs(IEnumerable<GenerationFact> facts) =>
        [.. facts.DistinctBy(fact => fact.Id).OrderBy(fact => fact.Id.Value, StringComparer.Ordinal)];

    internal static GenerationDiagnostic Diagnostic(string code, GenerationDiagnosticOutcome outcome, GenerationFact[] inputs, string message) => new()
    {
        Code = code, Severity = GenerationDiagnosticSeverity.Error, Outcome = outcome, Message = message,
        Subject = inputs.OfType<ArtifactSourcePlacementCandidateFact>().First().Subject,
        Source = inputs.FirstOrDefault(fact => fact.Evidence.Source is not null)?.Evidence.Source,
        Facts = [.. inputs.Select(fact => fact.Id)]
    };

    static bool Valid(ArtifactSourcePlacementCandidateFact candidate, SourcePlacementCandidateIndex index)
    {
        if (!ValidIdentity(candidate)) return false;
        var definition = candidate.Definition;
        if (!index.HasUnambiguousDeclaration(definition.Artifact) || !index.HasUnambiguousDeclaration(definition.Owner)) return false;
        return definition.Reason switch
        {
            ArtifactSourcePlacementReason.Self => definition.Owner == definition.Artifact,
            ArtifactSourcePlacementReason.ProducingCommand => IsEventProduction(definition) && index.HasProduction(definition),
            _ => false
        };
    }

    static bool ValidIdentity(ArtifactSourcePlacementCandidateFact candidate) =>
        SourcePlacementCandidateStructure.Supported(candidate.Definition) && GenerationFactDiscriminators.AreSupported(candidate) && candidate.Subject == candidate.Definition.Artifact.Subject;

    static bool IsEventProduction(ArtifactSourcePlacementCandidateDefinition definition) => definition.Artifact.Kind == ArtifactKind.Event && definition.Owner.Kind == ArtifactKind.Command;

    static GenerationFactRecord Record(ArtifactKey artifact, ArtifactPlacement placement, EvidenceStrength strength, GenerationFact[] inputs) => new()
    {
        Fact = new ArtifactPlacementFact
        {
            Id = new FactId { Value = $"generation:derive:source-owner-placement:1:{Encode(Structural.ArtifactKey(artifact))}" },
            Subject = artifact.Subject, Artifact = artifact, Placement = placement,
            Evidence = inputs.OfType<ArtifactSourcePlacementCandidateFact>().First(fact => fact.Evidence.Strength == strength).Evidence
        },
        Lineage = new() { Producer = Rule, Inputs = [.. inputs.Select(fact => fact.Id)], Evidence = [.. inputs.Select(fact => fact.Evidence)] }
    };

    static string Encode(string value) => string.Concat(value.Select(character => ((int)character).ToString("X4", CultureInfo.InvariantCulture)));

    sealed class Selection(GenerationFact[] facts, IArtifactSourcePlacementResolver? resolver)
    {
        readonly SourcePlacementCandidateIndex _index = new(facts);
        readonly List<GenerationFactRecord> _output = [];
        readonly List<GenerationDiagnostic> _diagnostics = [];

        public ArtifactUsageDerivationResult Derive()
        {
            List<GenerationFact> inputs = [];
            var candidates = facts.OfType<ArtifactSourcePlacementCandidateFact>().Select(candidate => candidate with { Definition = SourcePlacementCandidateStructure.Copy(candidate.Definition, key => key) });
            foreach (var group in candidates.GroupBy(fact => Structural.ArtifactKey(fact.Definition.Artifact), StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                var observations = group.ToArray();
                var support = _index.Inputs(observations);
                inputs.AddRange(support);
                SelectAndResolve(observations, support);
            }

            return new([.. CanonicalInputs(inputs).Select(fact => fact.Id)], [.. _output], [.. _diagnostics]);
        }

        void SelectAndResolve(ArtifactSourcePlacementCandidateFact[] candidates, GenerationFact[] inputs)
        {
            if (candidates.Any(candidate => !Valid(candidate, _index)))
            {
                _diagnostics.Add(Diagnostic(GenerationDiagnosticCodes.InvalidSourceOwnerCandidate, GenerationDiagnosticOutcome.Unsupported, inputs, "A source-owner candidate lacks an unambiguous declaration, supported policy or independent production proof"));
                return;
            }

            var strength = candidates.Min(candidate => candidate.Evidence.Strength);
            var strongest = candidates.Where(candidate => candidate.Evidence.Strength == strength).ToArray();
            if (strongest.Select(candidate => candidate.Definition.SliceKind).Distinct().Count() != 1)
            {
                _diagnostics.Add(Diagnostic(GenerationDiagnosticCodes.ConflictingSourceOwnerCandidates, GenerationDiagnosticOutcome.Conflict, inputs, "Equally strong source-owner candidates disagree on semantic slice kind"));
                return;
            }

            var definition = strongest[0].Definition;
            var producers = strongest.Where(candidate => candidate.Definition.Reason == ArtifactSourcePlacementReason.ProducingCommand).Select(candidate => candidate.Definition.Owner).Distinct().ToArray();
            Map(definition, producers.Length == 1 ? producers[0] : definition.Artifact, strength, inputs);
        }

        void Map(ArtifactSourcePlacementCandidateDefinition definition, ArtifactKey owner, EvidenceStrength strength, GenerationFact[] inputs)
        {
            if (resolver is null)
            {
                _diagnostics.Add(Diagnostic(GenerationDiagnosticCodes.SourcePlacementResolverUnavailable, GenerationDiagnosticOutcome.Unsupported, inputs, "Source-owner candidates require a host-owned fixed-snapshot source placement resolver"));
                return;
            }

            var resolution = resolver.Resolve(new() { Artifact = definition.Artifact, SourceOwner = owner, SliceKind = definition.SliceKind });
            var mapped = SourcePlacementResolutionValidation.Validate(resolution, definition.Artifact, definition.SliceKind, inputs);
            _diagnostics.AddRange(mapped.Diagnostics);
            if (mapped.Placement is not { } placement || HasCompletedConflict(definition.Artifact, mapped.Placement, inputs)) return;
            _output.Add(Record(definition.Artifact, placement, strength, inputs));
        }

        bool HasCompletedConflict(ArtifactKey artifact, ArtifactPlacement placement, GenerationFact[] inputs)
        {
            var completed = facts.OfType<ArtifactPlacementFact>().Where(fact => fact.Artifact == artifact).ToArray();
            if (completed.Length == 0) return false;
            var strength = completed.Min(fact => fact.Evidence.Strength);
            var effective = completed.Where(fact => fact.Evidence.Strength == strength).ToArray();
            if (effective.All(fact => Structural.Placement(fact.Placement) == Structural.Placement(placement))) return false;
            _diagnostics.Add(Diagnostic(GenerationDiagnosticCodes.ConflictingSourceOwnerCandidates, GenerationDiagnosticOutcome.Conflict, CanonicalInputs(inputs.Concat(effective)), "Selected source placement conflicts with effective completed placement evidence"));
            return true;
        }
    }
}
