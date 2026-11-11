// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Names the subject and family of an admitted fact for the diagnostics raised about it.
/// </summary>
internal static class GenerationFactDescription
{
    public static SubjectId DiagnosticSubject(GenerationFact fact) => fact switch
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

    public static string Family(GenerationFact fact) => fact switch
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
}
