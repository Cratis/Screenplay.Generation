// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

internal static class GenerationFactDispositionCalculator
{
    public static ImmutableArray<GenerationFactRecord> Calculate(
        IEnumerable<GenerationFactRecord> records,
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        var inputs = records.ToArray();
        var calculated = Calculate(inputs.Select(record => record.Fact), graph, coverage, diagnostics);
        return
        [
            .. calculated.Select((record, index) => record with { Lineage = inputs[index].Lineage })
        ];
    }

    public static ImmutableArray<GenerationFactRecord> Calculate(
        IEnumerable<GenerationFact> facts,
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        var admitted = facts.ToArray();
        var conflictingIdentities = admitted
            .Where(HasSupportedDiscriminators)
            .GroupBy(_ => _.Id.Value, StringComparer.Ordinal)
            .Where(group => group
                .Select(SemanticIdentity)
                .Distinct(StringComparer.Ordinal)
                .Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        return
        [
            .. admitted.Select(fact => Calculate(
                fact,
                conflictingIdentities.Contains(fact.Id.Value),
                graph,
                coverage,
                diagnostics))
        ];
    }

    static GenerationFactRecord Calculate(
        GenerationFact fact,
        bool hasConflictingIdentity,
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        if (!HasSupportedDiscriminators(fact))
        {
            return Omitted(fact, coverage, diagnostics);
        }

        if (hasConflictingIdentity ||
            IsConflicted(fact, graph, coverage) ||
            AssociatedDiagnostics(fact, coverage, diagnostics).Any(diagnostic => diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict))
        {
            return Conflicted(fact, coverage, diagnostics);
        }

        var granularDisposition = GranularDisposition(fact, graph, coverage, diagnostics);
        if (granularDisposition is not null)
        {
            return granularDisposition;
        }

        if (fact is ArtifactFact legacyArtifact &&
            graph.Artifacts
                .Where(resolved => resolved.Key == legacyArtifact.Definition.Key)
                .SelectMany(resolved => resolved.Variants)
                .Any(variant =>
                    Structural.Artifact(legacyArtifact.Definition) != Structural.Artifact(variant.Definition) &&
                    variant.SupportingFacts.Any(id => id == legacyArtifact.Id) &&
                    coverage.Lowered.Contains(GenerationFactSemanticKey.Artifact(variant.Definition))))
        {
            return new GenerationFactRecord
            {
                Fact = fact,
                Disposition = GenerationFactDisposition.ProvenanceOnly
            };
        }

        var key = GenerationFactSemanticKey.For(fact);
        if (key is not null && coverage.Lowered.Contains(key))
        {
            return new GenerationFactRecord
            {
                Fact = fact,
                Disposition = GenerationFactDisposition.Lowered
            };
        }

        if (fact is ArtifactPlacementFact placement && IsWeakerPlacement(placement, graph))
        {
            return new GenerationFactRecord
            {
                Fact = fact,
                Disposition = GenerationFactDisposition.ProvenanceOnly
            };
        }

        if (key is not null)
        {
            return Omitted(fact, coverage, diagnostics);
        }

        var diagnostic = new GenerationDiagnostic
        {
            Code = GenerationDiagnosticCodes.UnclassifiedGenerationFact,
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = GenerationDiagnosticOutcome.Unknown,
            Message = $"Admitted fact '{fact.Id.Value}' uses unclassifiable fact type '{fact.GetType().FullName}'",
            Source = fact.Evidence.Source,
            Subject = fact.Subject
        };
        return new GenerationFactRecord
        {
            Fact = fact,
            Disposition = GenerationFactDisposition.Unknown,
            Diagnostics = [diagnostic]
        };
    }

    static GenerationFactRecord Conflicted(
        GenerationFact fact,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        var associated = AssociatedDiagnostics(fact, coverage, diagnostics)
            .Where(_ => _.Outcome == GenerationDiagnosticOutcome.Conflict)
            .ToArray();
        if (associated.Length == 0)
        {
            associated =
            [
                new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.ConflictingGenerationFact,
                    Severity = GenerationDiagnosticSeverity.Error,
                    Outcome = GenerationDiagnosticOutcome.Conflict,
                    Message = $"Admitted fact '{fact.Id.Value}' participated in an unresolved semantic conflict",
                    Source = fact.Evidence.Source,
                    Subject = DiagnosticSubject(fact)
                }
            ];
        }

        return new GenerationFactRecord
        {
            Fact = fact,
            Disposition = GenerationFactDisposition.Conflicted,
            Diagnostics = CanonicalDiagnostics(associated)
        };
    }

    static GenerationFactRecord Omitted(
        GenerationFact fact,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        var associated = AssociatedDiagnostics(fact, coverage, diagnostics).ToList();
        if (fact is RelationshipFact relationship && HasSupportedDiscriminators(fact))
        {
            associated.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.UnsupportedRelationship,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = $"Relationship '{relationship.Definition.Key.Kind}' from '{relationship.Definition.Key.Source.Value}' to '{relationship.Definition.Key.Target.Value}' did not contribute to emitted Screenplay syntax and was omitted",
                Source = fact.Evidence.Source,
                Subject = relationship.Definition.Key.Source
            });
        }

        if (associated.Count == 0)
        {
            associated.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.OmittedGenerationFact,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = $"Admitted {FactFamily(fact)} fact '{fact.Id.Value}' did not contribute to emitted Screenplay syntax and was omitted",
                Source = fact.Evidence.Source,
                Subject = DiagnosticSubject(fact)
            });
        }

        return new GenerationFactRecord
        {
            Fact = fact,
            Disposition = GenerationFactDisposition.OmittedWithDiagnostic,
            Diagnostics = CanonicalDiagnostics(associated)
        };
    }

    static IEnumerable<GenerationDiagnostic> AssociatedDiagnostics(
        GenerationFact fact,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        var key = GenerationFactSemanticKey.For(fact);
        if (key is not null && coverage.Diagnostics.TryGetValue(key, out var loweringDiagnostics))
        {
            foreach (var diagnostic in loweringDiagnostics)
            {
                yield return diagnostic;
            }
        }

        foreach (var diagnostic in diagnostics.Where(diagnostic =>
                     diagnostic.Outcome is not null && HasExactFactIdentity(diagnostic, fact.Id.Value)))
        {
            yield return diagnostic;
        }
    }

    static GenerationFactRecord? GranularDisposition(
        GenerationFact fact,
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        var artifact = fact switch
        {
            ArtifactDeclarationFact declaration => declaration.Definition.Artifact,
            ArtifactMemberDeclarationFact member => member.Definition.Member.Artifact,
            ArtifactMemberTypeUseFact typeUse => typeUse.Definition.Member.Artifact,
            TypeUseBindingFact binding => binding.Definition.Member.Artifact,
            ArtifactMemberRoleFact role => role.Definition.Member.Artifact,
            _ => null
        };
        if (artifact is null)
        {
            return null;
        }

        var variants = graph.Artifacts
            .Where(resolved => resolved.Key == artifact)
            .SelectMany(resolved => resolved.Variants)
            .ToArray();
        var appliedVariants = variants
            .Where(variant => variant.SupportingFacts.Any(id => id == fact.Id))
            .ToArray();
        if (appliedVariants.Length == 0)
        {
            return Omitted(fact, coverage, diagnostics);
        }

        var lowered = appliedVariants.Any(variant =>
            coverage.Lowered.Contains(GenerationFactSemanticKey.Artifact(variant.Definition)));
        var disposition = fact is TypeUseBindingFact or ArtifactMemberRoleFact && lowered
            ? GenerationFactDisposition.Lowered
            : GenerationFactDisposition.ProvenanceOnly;
        return new GenerationFactRecord
        {
            Fact = fact,
            Disposition = disposition
        };
    }

    static bool IsConflicted(
        GenerationFact fact,
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverage coverage)
    {
        var semanticKey = GenerationFactSemanticKey.For(fact);
        if (semanticKey is not null && coverage.Conflicted.Contains(semanticKey))
        {
            return true;
        }

        return fact switch
        {
            ArtifactFact artifact => graph.Artifacts.Any(resolved =>
                resolved.IsConflicted &&
                Structural.ArtifactKey(resolved.Key) == Structural.ArtifactKey(artifact.Definition.Key) &&
                resolved.Variants.Any(variant => variant.SupportingFacts.Any(id => id == artifact.Id))),
            ArtifactPlacementFact placement => ConflictingPlacement(placement, graph),
            ArtifactDeclarationFact or
            ArtifactMemberDeclarationFact or
            ArtifactMemberTypeUseFact or
            TypeUseBindingFact or
            ArtifactMemberRoleFact => graph.Artifacts.Any(resolved =>
                resolved.IsConflicted &&
                resolved.Variants.Any(variant => variant.SupportingFacts.Any(id => id == fact.Id))),
            RelationshipFact relationship => graph.Relationships.Any(resolved =>
                resolved.IsConflicted &&
                Structural.RelationshipKey(resolved.Key) == Structural.RelationshipKey(relationship.Definition.Key) &&
                resolved.Definitions.Any(definition => Structural.Relationship(definition) == Structural.Relationship(relationship.Definition))),
            ConceptRepresentationFact representation => graph.ConceptRepresentations.Any(resolved =>
                resolved.IsConflicted &&
                resolved.Concept == representation.Definition.Concept &&
                resolved.Variants.Any(variant => Structural.ConceptRepresentation(variant.Definition) == Structural.ConceptRepresentation(representation.Definition))),
            ConceptAttributeFact attribute => graph.ConceptAttributes.Any(resolved =>
                resolved.IsConflicted &&
                resolved.Variants.Any(variant => Structural.ConceptAttribute(variant.Definition) == Structural.ConceptAttribute(attribute.Definition))),
            ConceptValidationRuleFact validation => graph.ConceptValidationRules.Any(resolved =>
                resolved.IsConflicted &&
                resolved.Variants.Any(variant => Structural.ConceptValidationRule(variant.Definition) == Structural.ConceptValidationRule(validation.Definition))),
            SpecificationScenarioFact scenario => graph.SpecificationScenarios.Any(resolved =>
                resolved.IsConflicted &&
                Structural.SpecificationScenarioKey(resolved.Key) == Structural.SpecificationScenarioKey(scenario.Definition.Key) &&
                resolved.Variants.Any(variant => Structural.SpecificationScenario(variant.Definition) == Structural.SpecificationScenario(scenario.Definition))),
            SpecificationStepFact step => graph.SpecificationSteps.Any(resolved =>
                resolved.IsConflicted &&
                Structural.SpecificationStepKey(resolved.Key) == Structural.SpecificationStepKey(step.Definition.Key) &&
                resolved.Variants.Any(variant => Structural.SpecificationStep(variant.Definition) == Structural.SpecificationStep(step.Definition))),
            SpecificationValueFact value => graph.SpecificationValues.Any(resolved =>
                resolved.IsConflicted &&
                Structural.SpecificationValueKey(resolved.Key) == Structural.SpecificationValueKey(value.Definition.Key) &&
                resolved.Variants.Any(variant => Structural.SpecificationValue(variant.Definition) == Structural.SpecificationValue(value.Definition))),
            _ => false
        };
    }

    static bool ConflictingPlacement(ArtifactPlacementFact fact, ResolvedApplicationGraph graph)
    {
        var resolved = graph.Placements.FirstOrDefault(_ =>
            Structural.ArtifactKey(_.Artifact) == Structural.ArtifactKey(fact.Artifact));
        if (resolved?.IsConflicted != true)
        {
            return false;
        }

        var placement = Structural.Placement(fact.Placement);
        return resolved.EffectiveVariants.Any(_ => Structural.Placement(_.Placement) == placement);
    }

    static bool IsWeakerPlacement(ArtifactPlacementFact fact, ResolvedApplicationGraph graph)
    {
        var resolved = graph.Placements.FirstOrDefault(_ =>
            Structural.ArtifactKey(_.Artifact) == Structural.ArtifactKey(fact.Artifact));
        if (resolved?.Variants.Any(_ => Structural.Placement(_.Placement) == Structural.Placement(fact.Placement)) != true)
        {
            return false;
        }

        var placement = Structural.Placement(fact.Placement);
        return resolved.EffectiveVariants.All(_ => Structural.Placement(_.Placement) != placement);
    }

    static bool HasExactFactIdentity(GenerationDiagnostic diagnostic, string factId) =>
        diagnostic.Message.Contains($"Fact '{factId}'", StringComparison.Ordinal) ||
        diagnostic.Message.Contains($"fact '{factId}'", StringComparison.Ordinal) ||
        diagnostic.Message.Contains($"Fact identity '{factId}'", StringComparison.Ordinal) ||
        (IsGranularDiagnostic(diagnostic.Code) && InputFactIds(diagnostic.Message).Contains(factId, StringComparer.Ordinal));

    static string[] InputFactIds(string message)
    {
        const string marker = "Input facts: ";
        var markerIndex = message.LastIndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return [];
        }

        return
        [
            .. message[(markerIndex + marker.Length)..]
                .Split(", ", StringSplitOptions.RemoveEmptyEntries)
                .Where(value => value.Length >= 2 && value[0] == '\'' && value[^1] == '\'')
                .Select(value => value[1..^1])
        ];
    }

    static bool IsGranularDiagnostic(string code) =>
        string.Equals(code, GenerationDiagnosticCodes.MissingTypeUseOwner, StringComparison.Ordinal) ||
        string.Equals(code, GenerationDiagnosticCodes.MissingTypeUseMember, StringComparison.Ordinal) ||
        string.Equals(code, GenerationDiagnosticCodes.MissingTypeUseTarget, StringComparison.Ordinal) ||
        string.Equals(code, GenerationDiagnosticCodes.ConflictingMemberTypeUse, StringComparison.Ordinal) ||
        string.Equals(code, GenerationDiagnosticCodes.ConflictingTypeUseTarget, StringComparison.Ordinal) ||
        string.Equals(code, GenerationDiagnosticCodes.ConflictingTypeUseDeclaration, StringComparison.Ordinal) ||
        string.Equals(code, GenerationDiagnosticCodes.UnsupportedTypeUseShape, StringComparison.Ordinal) ||
        string.Equals(code, GenerationDiagnosticCodes.ConflictingArtifactMember, StringComparison.Ordinal) ||
        string.Equals(code, GenerationDiagnosticCodes.IncompleteArtifactMember, StringComparison.Ordinal) ||
        string.Equals(code, GenerationDiagnosticCodes.InvalidGranularFactOwnership, StringComparison.Ordinal);

    static bool HasSupportedDiscriminators(GenerationFact fact)
    {
        if (!Supported(fact.Evidence.Strength, EvidenceStrength.Unknown))
        {
            return false;
        }

        return fact switch
        {
            ArtifactFact artifact => Supported(artifact.Definition.Key.Kind, ArtifactKind.Unknown),
            ArtifactPlacementFact placement =>
                Supported(placement.Artifact.Kind, ArtifactKind.Unknown) &&
                Supported(placement.Placement.SliceKind, GenerationSliceKind.Unknown),
            ArtifactDeclarationFact declaration => Supported(declaration.Definition.Artifact.Kind, ArtifactKind.Unknown),
            ArtifactMemberDeclarationFact member => Supported(member.Definition.Member.Artifact.Kind, ArtifactKind.Unknown),
            ArtifactMemberTypeUseFact typeUse =>
                Supported(typeUse.Definition.Member.Artifact.Kind, ArtifactKind.Unknown) &&
                typeUse.Definition.Type.Shape.All(shape => Supported(shape, TypeUseShapeKind.Unknown)),
            TypeUseBindingFact binding =>
                Supported(binding.Definition.Member.Artifact.Kind, ArtifactKind.Unknown) &&
                Supported(binding.Definition.Target.Kind, ArtifactKind.Unknown),
            ArtifactMemberRoleFact role =>
                Supported(role.Definition.Member.Artifact.Kind, ArtifactKind.Unknown) &&
                Supported(role.Definition.Role, ArtifactMemberRoleKind.Unknown),
            RelationshipFact relationship => Supported(relationship.Definition.Key.Kind, RelationshipKind.Unknown),
            ConceptRepresentationFact representation =>
                Supported(representation.Definition.Kind, ConceptRepresentationKind.Unknown) &&
                (representation.Definition.Primitive is not { } primitive || Supported(primitive, GenerationPrimitiveKind.Unknown)),
            ConceptAttributeFact attribute => Supported(attribute.Definition.Kind, ConceptAttributeKind.Unknown),
            ConceptValidationRuleFact validation => Supported(validation.Definition.Kind, ConceptValidationRuleKind.Unknown),
            SpecificationScenarioFact scenario => Supported(scenario.Definition.TargetArtifact.Kind, ArtifactKind.Unknown),
            SpecificationStepFact step =>
                Supported(step.Definition.Phase, SpecificationStepPhase.Unknown) &&
                Supported(step.Definition.Kind, SpecificationStepKind.Unknown) &&
                (step.Definition.Artifact is not { } artifact || Supported(artifact.Kind, ArtifactKind.Unknown)),
            SpecificationValueFact value => Supported(value.Definition.Kind, SpecificationValueKind.Unknown),
            _ => true
        };
    }

    static bool Supported<TEnum>(TEnum value, TEnum unknown)
        where TEnum : struct, Enum =>
        !EqualityComparer<TEnum>.Default.Equals(value, unknown) && Enum.IsDefined(value);

    static SubjectId DiagnosticSubject(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => artifact.Definition.Key.Subject,
        ArtifactPlacementFact placement => placement.Artifact.Subject,
        ArtifactDeclarationFact declaration => declaration.Definition.Artifact.Subject,
        ArtifactMemberDeclarationFact member => member.Definition.Member.Artifact.Subject,
        ArtifactMemberTypeUseFact typeUse => typeUse.Definition.Member.Artifact.Subject,
        TypeUseBindingFact binding => binding.Definition.Member.Artifact.Subject,
        ArtifactMemberRoleFact role => role.Definition.Member.Artifact.Subject,
        RelationshipFact relationship => relationship.Definition.Key.Source,
        ConceptRepresentationFact representation => representation.Definition.Concept,
        ConceptAttributeFact attribute => attribute.Definition.Concept,
        ConceptValidationRuleFact validation => validation.Definition.Concept,
        SpecificationScenarioFact scenario => scenario.Definition.Key.Scenario,
        SpecificationStepFact step => step.Definition.Key.Scenario.Scenario,
        SpecificationValueFact value => value.Definition.Key.Step.Scenario.Scenario,
        _ => fact.Subject
    };

    static string SemanticIdentity(GenerationFact fact) =>
        GenerationFactSemanticKey.For(fact) ?? fact.GetType().FullName ?? fact.GetType().Name;

    static string FactFamily(GenerationFact fact) => fact switch
    {
        ArtifactFact => "artifact",
        ArtifactPlacementFact => "artifact placement",
        ArtifactDeclarationFact => "artifact declaration",
        ArtifactMemberDeclarationFact => "artifact member declaration",
        ArtifactMemberTypeUseFact => "artifact member type use",
        TypeUseBindingFact => "type-use binding",
        ArtifactMemberRoleFact => "artifact member role",
        RelationshipFact => "relationship",
        ConceptRepresentationFact => "concept representation",
        ConceptAttributeFact => "concept attribute",
        ConceptValidationRuleFact => "concept validation rule",
        SpecificationScenarioFact => "specification scenario",
        SpecificationStepFact => "specification step",
        SpecificationValueFact => "specification value",
        _ => "generation"
    };

    static ImmutableArray<GenerationDiagnostic> CanonicalDiagnostics(IEnumerable<GenerationDiagnostic> diagnostics) =>
    [
        .. diagnostics
            .GroupBy(Canonical.Diagnostic, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.First())
    ];
}
