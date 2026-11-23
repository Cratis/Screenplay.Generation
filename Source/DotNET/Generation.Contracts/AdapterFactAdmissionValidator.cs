// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class AdapterFactAdmissionValidator
{
    public static void Validate(
        GenerationFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        switch (fact)
        {
            case ArtifactFact artifact:
                ArtifactFactAdmissionValidator.Validate(artifact, path, context);
                break;
            case ArtifactPlacementFact placement:
                ArtifactFactAdmissionValidator.Validate(placement, path, context);
                break;
            case ArtifactDeclarationFact declaration:
                GranularFactAdmissionValidator.Validate(declaration, path, context);
                break;
            case ArtifactMemberDeclarationFact member:
                GranularFactAdmissionValidator.Validate(member, path, context);
                break;
            case ArtifactMemberTypeUseFact typeUse:
                GranularFactAdmissionValidator.Validate(typeUse, path, context);
                break;
            case TypeUseBindingFact binding:
                GranularFactAdmissionValidator.Validate(binding, path, context);
                break;
            case ArtifactMemberRoleFact role:
                GranularFactAdmissionValidator.Validate(role, path, context);
                break;
            default:
                ValidateTypeUseOmission(fact, path, context);
                break;
        }
    }

    static void ValidateTypeUseOmission(GenerationFact fact, string path, AdapterContributionAdmissionContext context)
    {
        if (fact is TypeUseTargetOmittedFact omission)
        {
            GranularFactAdmissionValidator.Validate(omission, path, context);
        }
        else if (fact is MethodDiagnosticProjectionFact)
        {
            DiagnosticIntentAdmissionValidator.ValidateShape(fact, path, context);
        }
        else
        {
            ValidateDomainFamily(fact, path, context);
        }
    }

    static void ValidateDomainFamily(GenerationFact fact, string path, AdapterContributionAdmissionContext context)
    {
        switch (fact)
        {
            case DeclarationInputMemberDeclarationFact or DeclarationInputMemberTypeUseFact or EntryPointInputTransferFact:
                DeclarationInputMemberAdmissionValidator.Validate(fact, path, context);
                break;
            case BackendScopeFact scope:
                BackendScopeFactAdmissionValidator.Validate(scope, path, context);
                break;
            case ArtifactUsageDeclarationFact or MethodArtifactUsageFact or EntryPointFact:
                ArtifactUsageAdmissionValidator.Validate(fact, path, context);
                break;
            case RelationshipFact relationship:
                BackendScopeFactAdmissionValidator.ValidateKey(relationship.Definition.Key.SourceScope, $"{path}.Definition.Key.SourceScope", (fact.Id, fact.Subject), context);
                BackendScopeFactAdmissionValidator.ValidateKey(relationship.Definition.Key.TargetScope, $"{path}.Definition.Key.TargetScope", (fact.Id, fact.Subject), context);
                RelationshipFactAdmissionValidator.Validate(relationship, path, context);
                break;
            case ConceptRepresentationFact representation:
                ConceptFactAdmissionValidator.Validate(representation, path, context);
                break;
            case ConceptAttributeFact attribute:
                ConceptFactAdmissionValidator.Validate(attribute, path, context);
                break;
            case ConceptValidationRuleFact validation:
                ConceptFactAdmissionValidator.Validate(validation, path, context);
                break;
            default:
                ValidateSpecificationFamily(fact, path, context);
                break;
        }
    }

    static void ValidateSpecificationFamily(GenerationFact fact, string path, AdapterContributionAdmissionContext context)
    {
        switch (fact)
        {
            case SpecificationScenarioFact scenario:
                SpecificationFactAdmissionValidator.Validate(scenario, path, context);
                break;
            case SpecificationStepFact step:
                SpecificationFactAdmissionValidator.Validate(step, path, context);
                break;
            case SpecificationValueFact value:
                SpecificationFactAdmissionValidator.Validate(value, path, context);
                break;
        }
    }
}
