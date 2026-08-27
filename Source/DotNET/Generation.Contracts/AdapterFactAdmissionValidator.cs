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
            case RelationshipFact relationship:
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
