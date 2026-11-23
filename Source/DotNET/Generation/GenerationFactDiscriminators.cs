// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Decides whether every public discriminator carried by an admitted fact is a defined, known value.
/// </summary>
internal static class GenerationFactDiscriminators
{
    public static bool AreSupported(GenerationFact fact) =>
        Supported(fact.Evidence.Strength, EvidenceStrength.Unknown) && FamilySupported(fact);

    static bool FamilySupported(GenerationFact fact) => fact switch
    {
        MethodDiagnosticProjectionFact projection => DiagnosticIntentValidation.Valid(projection),
        OccurrenceOmissionDiagnosticFact candidate => OccurrenceOmissionDiagnosticValidation.Valid(candidate),
        ArtifactFact or
        ArtifactPlacementFact or
        ArtifactDeclarationFact or
        ArtifactMemberDeclarationFact or
        ArtifactMemberTypeUseFact or
        TypeUseTargetOmittedFact or
        TypeUseBindingFact or
        ArtifactMemberRoleFact => ArtifactFamilySupported(fact),
        ConceptRepresentationFact or
        ConceptAttributeFact or
        ConceptValidationRuleFact => ConceptFamilySupported(fact),
        SpecificationScenarioFact or
        SpecificationStepFact or
        SpecificationValueFact => SpecificationFamilySupported(fact),
        _ => UsageFamilySupported(fact)
    };

    static bool UsageFamilySupported(GenerationFact fact) => fact switch
    {
        ArtifactUsageDeclarationFact declaration => Supported(declaration.Definition.Artifact.Kind, ArtifactKind.Unknown),
        EntryPointFact entry => Supported(entry.Definition.Artifact.Kind, ArtifactKind.Unknown),
        MethodArtifactUsageFact usage => Supported(usage.Definition.Kind, RelationshipKind.Unknown),
        _ => ObservationFamilySupported(fact)
    };

    static bool ObservationFamilySupported(GenerationFact fact) => fact switch
    {
        DeclarationInputMemberTypeUseFact typeUse => typeUse.Definition.Type.Shape.All(shape => Supported(shape, TypeUseShapeKind.Unknown)),
        EntryPointInputTransferFact transfer => Supported(transfer.Definition.Artifact.Kind, ArtifactKind.Unknown) && Supported(transfer.Definition.Kind, RelationshipKind.Unknown),
        MethodExecutionClassificationFact classification => Supported(classification.Definition.Kind, MethodExecutionKind.Unknown),
        RelationshipFact relationship => Supported(relationship.Definition.Key.Kind, RelationshipKind.Unknown),
        _ => true
    };

    static bool ArtifactFamilySupported(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => Supported(artifact.Definition.Key.Kind, ArtifactKind.Unknown),
        ArtifactPlacementFact placement =>
            Supported(placement.Artifact.Kind, ArtifactKind.Unknown) &&
            Supported(placement.Placement.SliceKind, GenerationSliceKind.Unknown),
        ArtifactDeclarationFact declaration => Supported(declaration.Definition.Artifact.Kind, ArtifactKind.Unknown),
        _ => MemberFamilySupported(fact)
    };

    static bool MemberFamilySupported(GenerationFact fact) => fact switch
    {
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
        _ => TargetOmissionSupported(fact)
    };

    static bool TargetOmissionSupported(GenerationFact fact) => fact switch
    {
        TypeUseTargetOmittedFact omission => Supported(omission.Definition.Member.Artifact.Kind, ArtifactKind.Unknown),
        TypeUseBindingPolicyFact policy => TypeUseBindingPolicies.Supported(policy),
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
