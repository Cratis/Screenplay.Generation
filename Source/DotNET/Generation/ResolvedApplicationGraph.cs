// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Represents one distinct definition and all evidence that established it.
/// </summary>
public sealed record ResolvedArtifactVariant
{
    /// <summary>
    /// Gets the distinct artifact definition.
    /// </summary>
    public required ArtifactDefinition Definition { get; init; }

    /// <summary>
    /// Gets the canonical fact identities supporting the effective definition.
    /// </summary>
    public IReadOnlyList<FactId> SupportingFacts { get; init; } = [];

    /// <summary>
    /// Gets the ordered evidence supporting the definition.
    /// </summary>
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];
}

/// <summary>
/// Represents every distinct definition asserted for one artifact identity and role.
/// </summary>
public sealed record ResolvedArtifact
{
    /// <summary>
    /// Gets the artifact identity and role.
    /// </summary>
    public required ArtifactKey Key { get; init; }

    /// <summary>
    /// Gets the distinct definitions in deterministic order.
    /// </summary>
    public IReadOnlyList<ResolvedArtifactVariant> Variants { get; init; } = [];

    /// <summary>
    /// Gets whether incompatible definitions were asserted for the artifact.
    /// </summary>
    public bool IsConflicted => Variants.Count > 1;
}

/// <summary>
/// Represents one distinct artifact placement and all evidence establishing it.
/// </summary>
public sealed record ResolvedArtifactPlacementVariant
{
    /// <summary>
    /// Gets the asserted placement.
    /// </summary>
    public required ArtifactPlacement Placement { get; init; }

    /// <summary>
    /// Gets the ordered evidence supporting the placement.
    /// </summary>
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];

    /// <summary>
    /// Gets the strongest evidence supporting this placement.
    /// </summary>
    public EvidenceStrength Strength => Evidence.Min(_ => _.Strength);
}

/// <summary>
/// Represents every distinct placement asserted for one artifact role.
/// </summary>
public sealed record ResolvedArtifactPlacement
{
    /// <summary>
    /// Gets the artifact role being placed.
    /// </summary>
    public required ArtifactKey Artifact { get; init; }

    /// <summary>
    /// Gets all distinct placements in deterministic order.
    /// </summary>
    public IReadOnlyList<ResolvedArtifactPlacementVariant> Variants { get; init; } = [];

    /// <summary>
    /// Gets the strongest placement variants. Weaker heuristic placements remain as provenance but do not conflict
    /// with stronger configured or exact evidence.
    /// </summary>
    public IReadOnlyList<ResolvedArtifactPlacementVariant> EffectiveVariants
    {
        get
        {
            var strength = Variants.Min(_ => _.Strength);
            return [.. Variants.Where(_ => _.Strength == strength)];
        }
    }

    /// <summary>
    /// Gets whether equally strong incompatible placements were asserted for the artifact.
    /// </summary>
    public bool IsConflicted => EffectiveVariants.Count > 1;
}

/// <summary>
/// Represents one resolved semantic relationship and all evidence establishing it.
/// </summary>
public sealed record ResolvedRelationship
{
    /// <summary>
    /// Gets the relationship identity.
    /// </summary>
    public required RelationshipKey Key { get; init; }

    /// <summary>
    /// Gets the distinct relationship definitions in deterministic order.
    /// </summary>
    public IReadOnlyList<RelationshipDefinition> Definitions { get; init; } = [];

    /// <summary>
    /// Gets all ordered evidence supporting the relationship definitions.
    /// </summary>
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];

    /// <summary>
    /// Gets whether incompatible definitions were asserted for the relationship.
    /// </summary>
    public bool IsConflicted => Definitions.Count > 1;
}

/// <summary>
/// Represents one distinct concept representation and all evidence establishing it.
/// </summary>
public sealed record ResolvedConceptRepresentationVariant
{
    /// <summary>
    /// Gets the asserted concept representation.
    /// </summary>
    public required ConceptRepresentationDefinition Definition { get; init; }

    /// <summary>
    /// Gets the ordered evidence supporting the representation.
    /// </summary>
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];
}

/// <summary>
/// Represents every distinct representation asserted for one concept subject.
/// </summary>
public sealed record ResolvedConceptRepresentation
{
    /// <summary>
    /// Gets the concept subject.
    /// </summary>
    public required SubjectId Concept { get; init; }

    /// <summary>
    /// Gets the distinct representations in deterministic order.
    /// </summary>
    public IReadOnlyList<ResolvedConceptRepresentationVariant> Variants { get; init; } = [];

    /// <summary>
    /// Gets whether incompatible representations were asserted for the concept.
    /// </summary>
    public bool IsConflicted => Variants.Count > 1;
}

/// <summary>
/// Represents one distinct concept attribute and all evidence establishing it.
/// </summary>
public sealed record ResolvedConceptAttributeVariant
{
    /// <summary>
    /// Gets the asserted concept attribute.
    /// </summary>
    public required ConceptAttributeDefinition Definition { get; init; }

    /// <summary>
    /// Gets the ordered evidence supporting the attribute.
    /// </summary>
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];
}

/// <summary>
/// Represents every distinct definition asserted for one named concept attribute.
/// </summary>
public sealed record ResolvedConceptAttribute
{
    /// <summary>
    /// Gets the concept subject.
    /// </summary>
    public required SubjectId Concept { get; init; }

    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the distinct definitions in deterministic order.
    /// </summary>
    public IReadOnlyList<ResolvedConceptAttributeVariant> Variants { get; init; } = [];

    /// <summary>
    /// Gets whether incompatible definitions were asserted for the named attribute.
    /// </summary>
    public bool IsConflicted => Variants.Count > 1;
}

/// <summary>
/// Represents one distinct concept validation rule and all evidence establishing it.
/// </summary>
public sealed record ResolvedConceptValidationRuleVariant
{
    /// <summary>
    /// Gets the asserted concept validation rule.
    /// </summary>
    public required ConceptValidationRuleDefinition Definition { get; init; }

    /// <summary>
    /// Gets the ordered evidence supporting the rule.
    /// </summary>
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];
}

/// <summary>
/// Represents every distinct definition asserted for one concept validation rule identity.
/// </summary>
public sealed record ResolvedConceptValidationRule
{
    /// <summary>
    /// Gets the concept subject.
    /// </summary>
    public required SubjectId Concept { get; init; }

    /// <summary>
    /// Gets the stable adapter-authored rule identity.
    /// </summary>
    public required string RuleIdentity { get; init; }

    /// <summary>
    /// Gets the distinct definitions in deterministic order.
    /// </summary>
    public IReadOnlyList<ResolvedConceptValidationRuleVariant> Variants { get; init; } = [];

    /// <summary>
    /// Gets whether incompatible definitions were asserted for the rule identity.
    /// </summary>
    public bool IsConflicted => Variants.Count > 1;
}

/// <summary>
/// Represents the deterministic semantic graph resolved from all adapter contributions.
/// </summary>
public sealed record ResolvedApplicationGraph
{
    /// <summary>
    /// Gets the resolved artifacts in canonical order.
    /// </summary>
    public IReadOnlyList<ResolvedArtifact> Artifacts { get; init; } = [];

    /// <summary>
    /// Gets resolved concept representations in canonical subject order.
    /// </summary>
    public IReadOnlyList<ResolvedConceptRepresentation> ConceptRepresentations { get; init; } = [];

    /// <summary>
    /// Gets resolved concept attributes in canonical concept/name order.
    /// </summary>
    public IReadOnlyList<ResolvedConceptAttribute> ConceptAttributes { get; init; } = [];

    /// <summary>
    /// Gets resolved concept validation rules in canonical concept/rule identity order.
    /// </summary>
    public IReadOnlyList<ResolvedConceptValidationRule> ConceptValidationRules { get; init; } = [];

    /// <summary>
    /// Gets the resolved artifact placements in canonical order.
    /// </summary>
    public IReadOnlyList<ResolvedArtifactPlacement> Placements { get; init; } = [];

    /// <summary>
    /// Gets the resolved relationships in canonical order.
    /// </summary>
    public IReadOnlyList<ResolvedRelationship> Relationships { get; init; } = [];

    /// <summary>
    /// Gets resolved specification scenario assertions in canonical order.
    /// </summary>
    public IReadOnlyList<ResolvedSpecificationScenario> SpecificationScenarios { get; init; } = [];

    /// <summary>
    /// Gets resolved specification step assertions in canonical order.
    /// </summary>
    public IReadOnlyList<ResolvedSpecificationStep> SpecificationSteps { get; init; } = [];

    /// <summary>
    /// Gets resolved specification value assertions in canonical order.
    /// </summary>
    public IReadOnlyList<ResolvedSpecificationValue> SpecificationValues { get; init; } = [];

    /// <summary>
    /// Gets complete specification scenarios admitted atomically in canonical order.
    /// </summary>
    public IReadOnlyList<AdmittedSpecificationScenario> Specifications { get; init; } = [];

    /// <summary>
    /// Gets adapter and resolution diagnostics in canonical order.
    /// </summary>
    public IReadOnlyList<GenerationDiagnostic> Diagnostics { get; init; } = [];
}
