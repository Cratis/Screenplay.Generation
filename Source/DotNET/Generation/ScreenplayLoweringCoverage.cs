// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

internal static class GenerationFactSemanticKey
{
    public static string? For(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => Artifact(artifact.Definition),
        ArtifactPlacementFact placement => Placement(placement.Artifact, placement.Placement),
        ArtifactDeclarationFact declaration => ArtifactDeclaration(declaration.Definition),
        ArtifactMemberDeclarationFact member => ArtifactMemberDeclaration(member.Definition),
        ArtifactMemberTypeUseFact typeUse => ArtifactMemberTypeUse(typeUse.Definition),
        TypeUseBindingFact binding => TypeUseBinding(binding.Definition),
        ArtifactMemberRoleFact role => ArtifactMemberRole(role.Definition),
        RelationshipFact relationship => Relationship(relationship.Definition),
        ConceptRepresentationFact representation => ConceptRepresentation(representation.Definition),
        ConceptAttributeFact attribute => ConceptAttribute(attribute.Definition),
        ConceptValidationRuleFact validation => ConceptValidationRule(validation.Definition),
        SpecificationScenarioFact scenario => SpecificationScenario(scenario.Definition),
        SpecificationStepFact step => SpecificationStep(step.Definition),
        SpecificationValueFact value => SpecificationValue(value.Definition),
        _ => null
    };

    public static string Artifact(ArtifactDefinition definition) =>
        Structural.SemanticKey("artifact", Structural.Artifact(definition));

    public static string ArtifactDeclaration(ArtifactDeclarationDefinition definition) =>
        Structural.SemanticKey("artifact-declaration", Structural.ArtifactDeclaration(definition));

    public static string ArtifactMemberDeclaration(ArtifactMemberDeclarationDefinition definition) =>
        Structural.SemanticKey("artifact-member-declaration", Structural.ArtifactMemberDeclaration(definition));

    public static string ArtifactMemberTypeUse(ArtifactMemberTypeUseDefinition definition) =>
        Structural.SemanticKey("artifact-member-type-use", Structural.ArtifactMemberTypeUse(definition));

    public static string TypeUseBinding(TypeUseBindingDefinition definition) =>
        Structural.SemanticKey("type-use-binding", Structural.TypeUseBinding(definition));

    public static string ArtifactMemberRole(ArtifactMemberRoleDefinition definition) =>
        Structural.SemanticKey("artifact-member-role", Structural.ArtifactMemberRole(definition));

    public static string Placement(ArtifactKey artifact, ArtifactPlacement placement) =>
        Structural.SemanticKey("placement", Structural.ArtifactKey(artifact), Structural.Placement(placement));

    public static string Relationship(RelationshipDefinition definition) =>
        Structural.SemanticKey("relationship", Structural.Relationship(definition));

    public static string ConceptRepresentation(ConceptRepresentationDefinition definition) =>
        Structural.SemanticKey("concept-representation", Structural.ConceptRepresentation(definition));

    public static string ConceptAttribute(ConceptAttributeDefinition definition) =>
        Structural.SemanticKey("concept-attribute", Structural.ConceptAttribute(definition));

    public static string ConceptValidationRule(ConceptValidationRuleDefinition definition) =>
        Structural.SemanticKey("concept-validation-rule", Structural.ConceptValidationRule(definition));

    public static string SpecificationScenario(SpecificationScenarioDefinition definition) =>
        Structural.SemanticKey("specification-scenario", Structural.SpecificationScenario(definition));

    public static string SpecificationStep(SpecificationStepDefinition definition) =>
        Structural.SemanticKey("specification-step", Structural.SpecificationStep(definition));

    public static string SpecificationValue(SpecificationValueDefinition definition) =>
        Structural.SemanticKey("specification-value", Structural.SpecificationValue(definition));
}

internal sealed record ScreenplayLoweringCoverage
{
    public static ScreenplayLoweringCoverage Empty { get; } = new();

    public ImmutableHashSet<string> Lowered { get; init; } = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal);

    public ImmutableHashSet<string> Conflicted { get; init; } = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal);

    public ImmutableDictionary<string, ImmutableArray<GenerationDiagnostic>> Diagnostics { get; init; } =
        ImmutableDictionary<string, ImmutableArray<GenerationDiagnostic>>.Empty.WithComparers(StringComparer.Ordinal);
}

internal sealed class ScreenplayLoweringCoverageBuilder
{
    readonly Dictionary<string, List<GenerationDiagnostic>> _diagnostics = new(StringComparer.Ordinal);
    readonly HashSet<string> _lowered = new(StringComparer.Ordinal);
    readonly HashSet<string> _conflicted = new(StringComparer.Ordinal);

    public void Lowered(string key) => _lowered.Add(key);

    public void Omitted(string key, GenerationDiagnostic diagnostic) => AddDiagnostic(key, diagnostic);

    public void Conflicted(string key, GenerationDiagnostic diagnostic)
    {
        _conflicted.Add(key);
        AddDiagnostic(key, diagnostic);
    }

    public ScreenplayLoweringCoverage Build() => new()
    {
        Lowered = _lowered.ToImmutableHashSet(StringComparer.Ordinal),
        Conflicted = _conflicted.ToImmutableHashSet(StringComparer.Ordinal),
        Diagnostics = _diagnostics.ToImmutableDictionary(
            item => item.Key,
            item => item.Value
                .GroupBy(Canonical.Diagnostic, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToImmutableArray(),
            StringComparer.Ordinal)
    };

    void AddDiagnostic(string key, GenerationDiagnostic diagnostic)
    {
        if (!_diagnostics.TryGetValue(key, out var diagnostics))
        {
            diagnostics = [];
            _diagnostics.Add(key, diagnostics);
        }

        diagnostics.Add(diagnostic);
    }
}
