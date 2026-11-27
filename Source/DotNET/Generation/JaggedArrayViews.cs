// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Resolves the opt-in legacy printable view of a jagged array member type use.</summary>
static class JaggedArrayViews
{
    /// <summary>Gets whether the fact carries any jagged-array view data.</summary>
    /// <param name="fact">The fact to inspect.</param>
    /// <returns>True when the fact carries jagged-array view data.</returns>
    public static bool HasData(GenerationFact fact) =>
        fact is ArtifactMemberTypeUseFact typeUse && TypeUseCompatibilityProjectionValidation.HasLegacyJaggedArrayData(typeUse.Definition);

    /// <summary>Gets whether the fact validly requests the legacy jagged view with no competing compatibility projection.</summary>
    /// <param name="fact">The fact to inspect.</param>
    /// <param name="facts">All facts, searched for competing projections.</param>
    /// <returns>True when the view is validly requested.</returns>
    public static bool IsViewed(GenerationFact fact, IReadOnlyList<GenerationFact> facts) =>
        fact is ArtifactMemberTypeUseFact { Definition.UseLegacyJaggedArrayReference: true } typeUse &&
        TypeUseCompatibilityProjectionValidation.ValidLegacyJaggedArrayData(typeUse.Definition) &&
        !Projections(fact, facts).Any();

    /// <summary>Gets whether the fact's jagged-array data is absent, or valid and free of a competing projection.</summary>
    /// <param name="fact">The fact to inspect.</param>
    /// <param name="facts">All facts, searched for competing projections.</param>
    /// <returns>True when the data is absent or consistent.</returns>
    public static bool IsConsistent(GenerationFact fact, IReadOnlyList<GenerationFact> facts) =>
        !HasData(fact) ||
        (TypeUseCompatibilityProjectionValidation.ValidLegacyJaggedArrayData(((ArtifactMemberTypeUseFact)fact).Definition) &&
         (!((ArtifactMemberTypeUseFact)fact).Definition.UseLegacyJaggedArrayReference || !Projections(fact, facts).Any()));

    /// <summary>Gets the same-producer compatibility projections of a requested view observation.</summary>
    /// <param name="observation">The requested view observation.</param>
    /// <param name="facts">All facts to search.</param>
    /// <returns>The matching compatibility projections.</returns>
    public static IEnumerable<TypeUseCompatibilityProjectionFact> Projections(GenerationFact observation, IReadOnlyList<GenerationFact> facts) =>
        facts.OfType<TypeUseCompatibilityProjectionFact>().Where(projection => projection.Definition?.TypeUse == observation.Id &&
            projection.Subject == observation.Subject && projection.Evidence.Adapter == observation.Evidence.Adapter &&
            projection.Evidence.Source == observation.Evidence.Source && projection.Evidence.Strength == EvidenceStrength.Exact && observation.Evidence.Strength == EvidenceStrength.Exact);

    /// <summary>Gets the printable view: an unnamed collection of an unnamed element with only the outer optionality.</summary>
    /// <param name="fact">The member type use fact.</param>
    /// <returns>The printable view definition.</returns>
    public static TypeUseDefinition View(ArtifactMemberTypeUseFact fact) => new()
    {
        Name = string.Empty,
        Shape = [.. fact.Definition.Type.Shape[0] == TypeUseShapeKind.Optional ? new[] { TypeUseShapeKind.Optional } : [], TypeUseShapeKind.Collection, TypeUseShapeKind.Named]
    };

    /// <summary>Gets a key distinguishing view requests and origins between otherwise equal observations.</summary>
    /// <param name="fact">The member type use fact.</param>
    /// <returns>The distinguishing key.</returns>
    public static string Key(ArtifactMemberTypeUseFact fact) => HasData(fact)
        ? $"{fact.Definition.UseLegacyJaggedArrayReference}:{string.Join(',', (fact.Definition.CollectionOrigins ?? []).Cast<int>())}"
        : string.Empty;
}
