// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Decides whether every public discriminator carried by an admitted fact is a defined, known value.
/// </summary>
internal static class GenerationFactDiscriminators
{
    public static bool AreSupported(GenerationFact fact)
    {
        if (!Supported(fact.Evidence.Strength, EvidenceStrength.Unknown))
        {
            return false;
        }

        return fact switch
        {
            ArtifactFact or
            ArtifactPlacementFact or
            ArtifactDeclarationFact or
            ArtifactMemberDeclarationFact or
            ArtifactMemberTypeUseFact or
            TypeUseBindingFact or
            ArtifactMemberRoleFact => ArtifactFamilySupported(fact),
            RelationshipFact relationship => Supported(relationship.Definition.Key.Kind, RelationshipKind.Unknown),
            ConceptRepresentationFact or
            ConceptAttributeFact or
            ConceptValidationRuleFact => ConceptFamilySupported(fact),
            SpecificationScenarioFact or
            SpecificationStepFact or
            SpecificationValueFact => SpecificationFamilySupported(fact),
            _ => true
        };
    }

    static bool ArtifactFamilySupported(GenerationFact fact) => fact switch
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
        _ => true
    };

    static bool ConceptFamilySupported(GenerationFact fact) => fact switch
    {
        ConceptRepresentationFact representation =>
            Supported(representation.Definition.Kind, ConceptRepresentationKind.Unknown) &&
            (representation.Definition.Primitive is not { } primitive || Supported(primitive, GenerationPrimitiveKind.Unknown)),
        ConceptAttributeFact attribute => Supported(attribute.Definition.Kind, ConceptAttributeKind.Unknown),
        ConceptValidationRuleFact validation => Supported(validation.Definition.Kind, ConceptValidationRuleKind.Unknown),
        _ => true
    };

    static bool SpecificationFamilySupported(GenerationFact fact) => fact switch
    {
        SpecificationScenarioFact scenario => Supported(scenario.Definition.TargetArtifact.Kind, ArtifactKind.Unknown),
        SpecificationStepFact step =>
            Supported(step.Definition.Phase, SpecificationStepPhase.Unknown) &&
            Supported(step.Definition.Kind, SpecificationStepKind.Unknown) &&
            (step.Definition.Artifact is not { } artifact || Supported(artifact.Kind, ArtifactKind.Unknown)),
        SpecificationValueFact value => Supported(value.Definition.Kind, SpecificationValueKind.Unknown),
        _ => true
    };

    static bool Supported<TEnum>(TEnum value, TEnum unknown)
        where TEnum : struct, Enum =>
        !EqualityComparer<TEnum>.Default.Equals(value, unknown) && Enum.IsDefined(value);
}
