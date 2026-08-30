// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class GenerationFactDiscriminatorValidator
{
    public static GenerationFactDiscriminatorValidationResult Validate(IEnumerable<GenerationFact> facts)
    {
        var validFacts = new List<GenerationFact>();
        var rejectedFacts = new List<GenerationFact>();
        var diagnostics = new List<GenerationDiagnostic>();

        foreach (var fact in facts)
        {
            var diagnosticCount = diagnostics.Count;
            ValidateEvidenceStrength(fact, diagnostics);

            switch (fact)
            {
                case ArtifactFact artifact:
                    ValidateArtifactKind(fact, artifact.Definition.Key.Kind, diagnostics);
                    foreach (var property in artifact.Definition.Properties)
                    {
                        ValidateTypeReference(fact, property.Type, diagnostics);
                    }
                    break;
                case ArtifactPlacementFact placement:
                    ValidateArtifactKind(fact, placement.Artifact.Kind, diagnostics);
                    ValidateSliceKind(fact, placement.Placement.SliceKind, diagnostics);
                    break;
                case ArtifactDeclarationFact declaration:
                    ValidateArtifactKind(fact, declaration.Definition.Artifact.Kind, diagnostics);
                    break;
                case ArtifactMemberDeclarationFact member:
                    ValidateArtifactKind(fact, member.Definition.Member.Artifact.Kind, diagnostics);
                    break;
                case ArtifactMemberTypeUseFact typeUse:
                    ValidateArtifactKind(fact, typeUse.Definition.Member.Artifact.Kind, diagnostics);
                    ValidateTypeUseShape(fact, typeUse.Definition.Type.Shape, diagnostics);
                    break;
                case TypeUseBindingFact binding:
                    ValidateArtifactKind(fact, binding.Definition.Member.Artifact.Kind, diagnostics);
                    ValidateArtifactKind(fact, binding.Definition.Target.Kind, diagnostics);
                    break;
                case ArtifactMemberRoleFact role:
                    ValidateArtifactKind(fact, role.Definition.Member.Artifact.Kind, diagnostics);
                    ValidateArtifactMemberRole(fact, role.Definition.Role, diagnostics);
                    break;
                case RelationshipFact relationship:
                    ValidateRelationshipKind(fact, relationship.Definition.Key.Kind, diagnostics);
                    break;
                case ConceptRepresentationFact representation:
                    ValidateConceptRepresentationKind(fact, representation.Definition.Kind, diagnostics);
                    if (representation.Definition.Primitive is { } primitive)
                    {
                        ValidatePrimitiveKind(fact, primitive, diagnostics);
                    }
                    break;
                case ConceptAttributeFact attribute:
                    ValidateConceptAttributeKind(fact, attribute.Definition.Kind, diagnostics);
                    break;
                case ConceptValidationRuleFact validationRule:
                    ValidateConceptValidationRuleKind(fact, validationRule.Definition.Kind, diagnostics);
                    break;
                case SpecificationScenarioFact scenario:
                    ValidateArtifactKind(fact, scenario.Definition.TargetArtifact.Kind, diagnostics);
                    break;
                case SpecificationStepFact step:
                    ValidateSpecificationStepPhase(fact, step.Definition.Phase, diagnostics);
                    ValidateSpecificationStepKind(fact, step.Definition.Kind, diagnostics);
                    if (step.Definition.Artifact is { } stepArtifact)
                    {
                        ValidateArtifactKind(fact, stepArtifact.Kind, diagnostics);
                    }
                    break;
                case SpecificationValueFact value:
                    ValidateSpecificationValueKind(fact, value.Definition.Kind, diagnostics);
                    if (value.Definition.Type is not null)
                    {
                        ValidateTypeReference(fact, value.Definition.Type, diagnostics);
                    }
                    break;
            }

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

    static void ValidateArtifactKind(GenerationFact fact, ArtifactKind kind, List<GenerationDiagnostic> diagnostics)
    {
        if (kind == ArtifactKind.Unknown || !Enum.IsDefined(kind))
        {
            diagnostics.Add(Unsupported(
                fact,
                GenerationDiagnosticCodes.UnsupportedArtifactKind,
                nameof(ArtifactKind),
                (int)kind,
                kind == ArtifactKind.Unknown));
        }
    }

    static void ValidateTypeReference(
        GenerationFact fact,
        TypeReferenceDefinition type,
        List<GenerationDiagnostic> diagnostics)
    {
        if (type.TargetArtifactKind is { } targetArtifactKind)
        {
            ValidateArtifactKind(fact, targetArtifactKind, diagnostics);
        }
    }

    static void ValidateSliceKind(GenerationFact fact, GenerationSliceKind kind, List<GenerationDiagnostic> diagnostics)
    {
        if (kind == GenerationSliceKind.Unknown || !Enum.IsDefined(kind))
        {
            diagnostics.Add(Unsupported(
                fact,
                GenerationDiagnosticCodes.UnsupportedSliceKind,
                nameof(GenerationSliceKind),
                (int)kind,
                kind == GenerationSliceKind.Unknown));
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
            if (kind == TypeUseShapeKind.Unknown || !Enum.IsDefined(kind))
            {
                diagnostics.Add(Unsupported(
                    fact,
                    GenerationDiagnosticCodes.UnsupportedTypeUseShapeKind,
                    nameof(TypeUseShapeKind),
                    (int)kind,
                    kind == TypeUseShapeKind.Unknown));
            }
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

    static void ValidateArtifactMemberRole(
        GenerationFact fact,
        ArtifactMemberRoleKind role,
        List<GenerationDiagnostic> diagnostics)
    {
        if (role == ArtifactMemberRoleKind.Unknown || !Enum.IsDefined(role))
        {
            diagnostics.Add(Unsupported(
                fact,
                GenerationDiagnosticCodes.UnsupportedArtifactMemberRoleKind,
                nameof(ArtifactMemberRoleKind),
                (int)role,
                role == ArtifactMemberRoleKind.Unknown));
        }
    }

    static void ValidateRelationshipKind(GenerationFact fact, RelationshipKind kind, List<GenerationDiagnostic> diagnostics)
    {
        if (kind == RelationshipKind.Unknown || !Enum.IsDefined(kind))
        {
            diagnostics.Add(Unsupported(
                fact,
                GenerationDiagnosticCodes.UnsupportedRelationshipKind,
                nameof(RelationshipKind),
                (int)kind,
                kind == RelationshipKind.Unknown));
        }
    }

    static void ValidateConceptRepresentationKind(
        GenerationFact fact,
        ConceptRepresentationKind kind,
        List<GenerationDiagnostic> diagnostics)
    {
        if (kind == ConceptRepresentationKind.Unknown || !Enum.IsDefined(kind))
        {
            diagnostics.Add(Unsupported(
                fact,
                GenerationDiagnosticCodes.UnsupportedConceptRepresentationKind,
                nameof(ConceptRepresentationKind),
                (int)kind,
                kind == ConceptRepresentationKind.Unknown));
        }
    }

    static void ValidatePrimitiveKind(GenerationFact fact, GenerationPrimitiveKind kind, List<GenerationDiagnostic> diagnostics)
    {
        if (kind == GenerationPrimitiveKind.Unknown || !Enum.IsDefined(kind))
        {
            diagnostics.Add(Unsupported(
                fact,
                GenerationDiagnosticCodes.UnsupportedPrimitiveKind,
                nameof(GenerationPrimitiveKind),
                (int)kind,
                kind == GenerationPrimitiveKind.Unknown));
        }
    }

    static void ValidateConceptAttributeKind(GenerationFact fact, ConceptAttributeKind kind, List<GenerationDiagnostic> diagnostics)
    {
        if (kind == ConceptAttributeKind.Unknown || !Enum.IsDefined(kind))
        {
            diagnostics.Add(Unsupported(
                fact,
                GenerationDiagnosticCodes.UnsupportedConceptAttributeKind,
                nameof(ConceptAttributeKind),
                (int)kind,
                kind == ConceptAttributeKind.Unknown));
        }
    }

    static void ValidateConceptValidationRuleKind(
        GenerationFact fact,
        ConceptValidationRuleKind kind,
        List<GenerationDiagnostic> diagnostics)
    {
        if (kind == ConceptValidationRuleKind.Unknown || !Enum.IsDefined(kind))
        {
            diagnostics.Add(Unsupported(
                fact,
                GenerationDiagnosticCodes.UnsupportedConceptValidationRuleKind,
                nameof(ConceptValidationRuleKind),
                (int)kind,
                kind == ConceptValidationRuleKind.Unknown));
        }
    }

    static void ValidateSpecificationStepPhase(
        GenerationFact fact,
        SpecificationStepPhase phase,
        List<GenerationDiagnostic> diagnostics)
    {
        if (phase == SpecificationStepPhase.Unknown || !Enum.IsDefined(phase))
        {
            diagnostics.Add(Unsupported(
                fact,
                GenerationDiagnosticCodes.UnsupportedSpecificationStepPhase,
                nameof(SpecificationStepPhase),
                (int)phase,
                phase == SpecificationStepPhase.Unknown));
        }
    }

    static void ValidateSpecificationStepKind(
        GenerationFact fact,
        SpecificationStepKind kind,
        List<GenerationDiagnostic> diagnostics)
    {
        if (kind == SpecificationStepKind.Unknown || !Enum.IsDefined(kind))
        {
            diagnostics.Add(Unsupported(
                fact,
                GenerationDiagnosticCodes.UnsupportedSpecificationStepKind,
                nameof(SpecificationStepKind),
                (int)kind,
                kind == SpecificationStepKind.Unknown));
        }
    }

    static void ValidateSpecificationValueKind(
        GenerationFact fact,
        SpecificationValueKind kind,
        List<GenerationDiagnostic> diagnostics)
    {
        if (kind == SpecificationValueKind.Unknown || !Enum.IsDefined(kind))
        {
            diagnostics.Add(Unsupported(
                fact,
                GenerationDiagnosticCodes.UnsupportedSpecificationValueKind,
                nameof(SpecificationValueKind),
                (int)kind,
                kind == SpecificationValueKind.Unknown));
        }
    }

    static void ValidateEvidenceStrength(GenerationFact fact, List<GenerationDiagnostic> diagnostics)
    {
        var strength = fact.Evidence.Strength;
        if (strength == EvidenceStrength.Unknown || !Enum.IsDefined(strength))
        {
            diagnostics.Add(Unsupported(
                fact,
                GenerationDiagnosticCodes.UnsupportedEvidenceStrength,
                nameof(EvidenceStrength),
                (int)strength,
                strength == EvidenceStrength.Unknown));
        }
    }

    static GenerationDiagnostic Unsupported(
        GenerationFact fact,
        string code,
        string discriminator,
        int value,
        bool isUnknown) => new()
    {
        Code = code,
        Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = isUnknown ? GenerationDiagnosticOutcome.Unknown : GenerationDiagnosticOutcome.Unsupported,
        Message = $"Fact '{fact.Id.Value}' from adapter '{fact.Evidence.Adapter.Id}' uses {(isUnknown ? "unknown" : "undefined")} {discriminator} value '{value}'; the affected fact was omitted",
        Facts = [fact.Id],
        Source = fact.Evidence.Source,
        Subject = fact.Subject
    };
}

sealed record GenerationFactDiscriminatorValidationResult(
    GenerationFact[] Facts,
    GenerationFact[] RejectedFacts,
    GenerationDiagnostic[] Diagnostics);
