// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Generation;

static class GenerationFactDiscriminatorValidator
{
    static readonly DiscriminatorRule<EvidenceStrength> _evidenceStrength = new(EvidenceStrength.Unknown, GenerationDiagnosticCodes.UnsupportedEvidenceStrength);
    static readonly DiscriminatorRule<ArtifactKind> _artifactKind = new(ArtifactKind.Unknown, GenerationDiagnosticCodes.UnsupportedArtifactKind);
    static readonly DiscriminatorRule<GenerationSliceKind> _sliceKind = new(GenerationSliceKind.Unknown, GenerationDiagnosticCodes.UnsupportedSliceKind);
    static readonly DiscriminatorRule<TypeUseShapeKind> _typeUseShapeKind = new(TypeUseShapeKind.Unknown, GenerationDiagnosticCodes.UnsupportedTypeUseShapeKind);
    static readonly DiscriminatorRule<ArtifactMemberRoleKind> _artifactMemberRoleKind = new(ArtifactMemberRoleKind.Unknown, GenerationDiagnosticCodes.UnsupportedArtifactMemberRoleKind);
    static readonly DiscriminatorRule<RelationshipKind> _relationshipKind = new(RelationshipKind.Unknown, GenerationDiagnosticCodes.UnsupportedRelationshipKind);
    static readonly DiscriminatorRule<ConceptRepresentationKind> _conceptRepresentationKind = new(ConceptRepresentationKind.Unknown, GenerationDiagnosticCodes.UnsupportedConceptRepresentationKind);
    static readonly DiscriminatorRule<GenerationPrimitiveKind> _primitiveKind = new(GenerationPrimitiveKind.Unknown, GenerationDiagnosticCodes.UnsupportedPrimitiveKind);
    static readonly DiscriminatorRule<ConceptAttributeKind> _conceptAttributeKind = new(ConceptAttributeKind.Unknown, GenerationDiagnosticCodes.UnsupportedConceptAttributeKind);
    static readonly DiscriminatorRule<ConceptValidationRuleKind> _conceptValidationRuleKind = new(ConceptValidationRuleKind.Unknown, GenerationDiagnosticCodes.UnsupportedConceptValidationRuleKind);
    static readonly DiscriminatorRule<SpecificationStepPhase> _specificationStepPhase = new(SpecificationStepPhase.Unknown, GenerationDiagnosticCodes.UnsupportedSpecificationStepPhase);
    static readonly DiscriminatorRule<SpecificationStepKind> _specificationStepKind = new(SpecificationStepKind.Unknown, GenerationDiagnosticCodes.UnsupportedSpecificationStepKind);
    static readonly DiscriminatorRule<SpecificationValueKind> _specificationValueKind = new(SpecificationValueKind.Unknown, GenerationDiagnosticCodes.UnsupportedSpecificationValueKind);

    public static GenerationFactDiscriminatorValidationResult Validate(IEnumerable<GenerationFact> facts)
    {
        var validFacts = new List<GenerationFact>();
        var rejectedFacts = new List<GenerationFact>();
        var diagnostics = new List<GenerationDiagnostic>();

        foreach (var fact in facts)
        {
            var diagnosticCount = diagnostics.Count;
            ValidateFact(fact, diagnostics);

            if (diagnostics.Count == diagnosticCount)
            {
                validFacts.Add(fact);
            }
            else
            {
                rejectedFacts.Add(fact);
            }
        }

        return new([.. validFacts], [.. rejectedFacts], [.. diagnostics]);
    }

    static void ValidateFact(GenerationFact fact, List<GenerationDiagnostic> diagnostics)
    {
        Validate(fact, fact.Evidence.Strength, _evidenceStrength, diagnostics);

        switch (fact)
        {
            case ArtifactFact or
                ArtifactPlacementFact or
                ArtifactDeclarationFact or
                ArtifactMemberDeclarationFact or
                ArtifactMemberTypeUseFact or
                TypeUseBindingFact or
                ArtifactMemberRoleFact:
                ValidateArtifactFamily(fact, diagnostics);
                break;
            case RelationshipFact relationship:
                Validate(fact, relationship.Definition.Key.Kind, _relationshipKind, diagnostics);
                break;
            case ConceptRepresentationFact or ConceptAttributeFact or ConceptValidationRuleFact:
                ValidateConceptFamily(fact, diagnostics);
                break;
            case SpecificationScenarioFact or SpecificationStepFact or SpecificationValueFact:
                ValidateSpecificationFamily(fact, diagnostics);
                break;
        }
    }

    static void ValidateArtifactFamily(GenerationFact fact, List<GenerationDiagnostic> diagnostics)
    {
        switch (fact)
        {
            case ArtifactFact artifact:
                ValidateArtifact(fact, artifact.Definition, diagnostics);
                break;
            case ArtifactPlacementFact placement:
                Validate(fact, placement.Artifact.Kind, _artifactKind, diagnostics);
                Validate(fact, placement.Placement.SliceKind, _sliceKind, diagnostics);
                break;
            case ArtifactDeclarationFact declaration:
                Validate(fact, declaration.Definition.Artifact.Kind, _artifactKind, diagnostics);
                break;
            case ArtifactMemberDeclarationFact member:
                Validate(fact, member.Definition.Member.Artifact.Kind, _artifactKind, diagnostics);
                break;
            case ArtifactMemberTypeUseFact typeUse:
                Validate(fact, typeUse.Definition.Member.Artifact.Kind, _artifactKind, diagnostics);
                ValidateTypeUseShape(fact, typeUse.Definition.Type.Shape, diagnostics);
                break;
            case TypeUseBindingFact binding:
                Validate(fact, binding.Definition.Member.Artifact.Kind, _artifactKind, diagnostics);
                Validate(fact, binding.Definition.Target.Kind, _artifactKind, diagnostics);
                break;
            case ArtifactMemberRoleFact role:
                Validate(fact, role.Definition.Member.Artifact.Kind, _artifactKind, diagnostics);
                Validate(fact, role.Definition.Role, _artifactMemberRoleKind, diagnostics);
                break;
        }
    }

    static void ValidateArtifact(GenerationFact fact, ArtifactDefinition definition, List<GenerationDiagnostic> diagnostics)
    {
        Validate(fact, definition.Key.Kind, _artifactKind, diagnostics);
        foreach (var property in definition.Properties)
        {
            ValidateTypeReference(fact, property.Type, diagnostics);
        }
    }

    static void ValidateConceptFamily(GenerationFact fact, List<GenerationDiagnostic> diagnostics)
    {
        switch (fact)
        {
            case ConceptRepresentationFact representation:
                Validate(fact, representation.Definition.Kind, _conceptRepresentationKind, diagnostics);
                if (representation.Definition.Primitive is { } primitive)
                {
                    Validate(fact, primitive, _primitiveKind, diagnostics);
                }

                break;
            case ConceptAttributeFact attribute:
                Validate(fact, attribute.Definition.Kind, _conceptAttributeKind, diagnostics);
                break;
            case ConceptValidationRuleFact validationRule:
                Validate(fact, validationRule.Definition.Kind, _conceptValidationRuleKind, diagnostics);
                break;
        }
    }

    static void ValidateSpecificationFamily(GenerationFact fact, List<GenerationDiagnostic> diagnostics)
    {
        switch (fact)
        {
            case SpecificationScenarioFact scenario:
                Validate(fact, scenario.Definition.TargetArtifact.Kind, _artifactKind, diagnostics);
                break;
            case SpecificationStepFact step:
                ValidateStep(fact, step.Definition, diagnostics);
                break;
            case SpecificationValueFact value:
                Validate(fact, value.Definition.Kind, _specificationValueKind, diagnostics);
                if (value.Definition.Type is not null)
                {
                    ValidateTypeReference(fact, value.Definition.Type, diagnostics);
                }

                break;
        }
    }

    static void ValidateStep(GenerationFact fact, SpecificationStepDefinition step, List<GenerationDiagnostic> diagnostics)
    {
        Validate(fact, step.Phase, _specificationStepPhase, diagnostics);
        Validate(fact, step.Kind, _specificationStepKind, diagnostics);
        if (step.Artifact is { } stepArtifact)
        {
            Validate(fact, stepArtifact.Kind, _artifactKind, diagnostics);
        }
    }

    static void ValidateTypeReference(
        GenerationFact fact,
        TypeReferenceDefinition type,
        List<GenerationDiagnostic> diagnostics)
    {
        if (type.TargetArtifactKind is { } targetArtifactKind)
        {
            Validate(fact, targetArtifactKind, _artifactKind, diagnostics);
        }
    }

    static void ValidateTypeUseShape(
        GenerationFact fact,
        IEnumerable<TypeUseShapeKind> shape,
        List<GenerationDiagnostic> diagnostics)
    {
        var nodes = shape.ToArray();
        foreach (var kind in nodes)
        {
            Validate(fact, kind, _typeUseShapeKind, diagnostics);
        }

        var nodesAreDefined = nodes.All(kind =>
            kind != TypeUseShapeKind.Unknown && Enum.IsDefined(kind));
        var hasExactNamedTerminal = nodes.Length > 0 &&
                                    nodes[^1] == TypeUseShapeKind.Named &&
                                    nodes[..^1].All(kind => kind != TypeUseShapeKind.Named);
        if (nodesAreDefined && !hasExactNamedTerminal)
        {
            diagnostics.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.UnsupportedTypeUseShape,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = nodes.Length == 0
                    ? GenerationDiagnosticOutcome.Unknown
                    : GenerationDiagnosticOutcome.Unsupported,
                Message = $"Fact '{fact.Id.Value}' uses malformed exact type-use shape '{Shape(nodes)}'; the affected fact was omitted",
                Facts = [fact.Id],
                Source = fact.Evidence.Source,
                Subject = fact.Subject
            });
        }
    }

    static string Shape(TypeUseShapeKind[] shape) => shape.Length == 0
        ? "<empty>"
        : string.Join(" -> ", shape);

    static void Validate<TEnum>(
        GenerationFact fact,
        TEnum value,
        DiscriminatorRule<TEnum> rule,
        List<GenerationDiagnostic> diagnostics)
        where TEnum : struct, Enum
    {
        var isUnknown = EqualityComparer<TEnum>.Default.Equals(value, rule.Unknown);
        if (isUnknown || !Enum.IsDefined(value))
        {
            var number = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            var problem = $"{(isUnknown ? "unknown" : "undefined")} {typeof(TEnum).Name} value '{number}'";
            diagnostics.Add(Unsupported(fact, rule.Code, problem, isUnknown));
        }
    }

    static GenerationDiagnostic Unsupported(GenerationFact fact, string code, string problem, bool isUnknown) => new()
    {
        Code = code,
        Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = isUnknown ? GenerationDiagnosticOutcome.Unknown : GenerationDiagnosticOutcome.Unsupported,
        Message = $"Fact '{fact.Id.Value}' from adapter '{fact.Evidence.Adapter.Id}' uses {problem}; the affected fact was omitted",
        Facts = [fact.Id],
        Source = fact.Evidence.Source,
        Subject = fact.Subject
    };

    sealed record DiscriminatorRule<TEnum>(TEnum Unknown, string Code)
        where TEnum : struct, Enum;
}

sealed record GenerationFactDiscriminatorValidationResult(
    GenerationFact[] Facts,
    GenerationFact[] RejectedFacts,
    GenerationDiagnostic[] Diagnostics);
