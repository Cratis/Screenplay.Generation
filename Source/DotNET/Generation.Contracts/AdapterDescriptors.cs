// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines the source language understood by an adapter.
/// </summary>
public enum AdapterSourceLanguage
{
    /// <summary>
    /// The source language is unknown or unsupported.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The adapter consumes source-neutral contributions and requires no source-language host.
    /// </summary>
    SourceIndependent = 0,

    /// <summary>
    /// C# source.
    /// </summary>
    CSharp = 1
}

/// <summary>
/// Defines the semantic concern owned by an adapter.
/// </summary>
public enum AdapterCategory
{
    /// <summary>
    /// The adapter category is unknown or unsupported.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// An application framework that spans several semantic concerns.
    /// </summary>
    ApplicationFramework = 0,

    /// <summary>
    /// Event-sourcing behavior.
    /// </summary>
    EventSourcing = 1,

    /// <summary>
    /// Event storage behavior.
    /// </summary>
    EventStore = 2,

    /// <summary>
    /// Messaging behavior.
    /// </summary>
    Messaging = 3,

    /// <summary>
    /// Domain concept or strongly typed value behavior.
    /// </summary>
    Concepts = 4,

    /// <summary>
    /// Validation behavior.
    /// </summary>
    Validation = 5,

    /// <summary>
    /// Semantics established only by an explicit integration between otherwise independent APIs.
    /// </summary>
    Integration = 6,

    /// <summary>
    /// A compatibility descriptor wrapping an adapter that predates structured categories.
    /// </summary>
    Legacy = 7
}

/// <summary>
/// Defines a source-neutral host capability an adapter can require.
/// </summary>
public enum AdapterHostCapability
{
    /// <summary>
    /// The host capability is unknown or unsupported.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Access to authoritative authored source.
    /// </summary>
    AuthoredSource = 0,

    /// <summary>
    /// Access to stable locations within authoritative authored source.
    /// </summary>
    StableSourceLocations = 1,

    /// <summary>
    /// Access to source-language semantic analysis.
    /// </summary>
    SemanticAnalysis = 2,

    /// <summary>
    /// Access to the selected project's referenced source projects.
    /// </summary>
    ProjectReferences = 3
}

/// <summary>
/// Defines one existing neutral fact family an adapter can emit.
/// </summary>
public enum GenerationFactCapability
{
    /// <summary>
    /// The emitted fact capability is unknown or unsupported.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// <see cref="ArtifactFact"/> facts.
    /// </summary>
    Artifact = 0,

    /// <summary>
    /// <see cref="ArtifactPlacementFact"/> facts.
    /// </summary>
    ArtifactPlacement = 1,

    /// <summary>
    /// <see cref="RelationshipFact"/> facts.
    /// </summary>
    Relationship = 2,

    /// <summary>
    /// <see cref="ConceptRepresentationFact"/> facts.
    /// </summary>
    ConceptRepresentation = 3,

    /// <summary>
    /// <see cref="ConceptAttributeFact"/> facts.
    /// </summary>
    ConceptAttribute = 4,

    /// <summary>
    /// <see cref="ConceptValidationRuleFact"/> facts.
    /// </summary>
    ConceptValidationRule = 5,

    /// <summary>
    /// <see cref="SpecificationScenarioFact"/> facts.
    /// </summary>
    SpecificationScenario = 6,

    /// <summary>
    /// <see cref="SpecificationStepFact"/> facts.
    /// </summary>
    SpecificationStep = 7,

    /// <summary>
    /// <see cref="SpecificationValueFact"/> facts.
    /// </summary>
    SpecificationValue = 8
}

/// <summary>
/// Identifies one source-neutral API capability that an adapter requires and its probe can prove.
/// </summary>
public sealed record AdapterApiCapability
{
    /// <summary>
    /// Gets the stable normalized capability identity.
    /// </summary>
    public required string Id { get; init; }
}

/// <summary>
/// Describes the inclusive and exclusive Generation package versions supported by an adapter.
/// </summary>
/// <remarks>
/// This range is distinct from <see cref="AdapterIdentity.Version"/>, which identifies the adapter implementation
/// that produced a contribution. A missing upper bound intentionally means that no upper bound is declared.
/// </remarks>
public sealed record GenerationVersionRange
{
    /// <summary>
    /// Gets a range that accepts every nonnegative Generation version.
    /// </summary>
    public static GenerationVersionRange Any { get; } = new();

    /// <summary>
    /// Gets the minimum supported Generation version, inclusive.
    /// </summary>
    public Version MinimumInclusive { get; init; } = new(0, 0);

    /// <summary>
    /// Gets the maximum supported Generation version, exclusive, or <see langword="null"/> when unbounded.
    /// </summary>
    public Version? MaximumExclusive { get; init; }
}

/// <summary>
/// Describes one trusted adapter independently from any source-language runner.
/// </summary>
public sealed record AdapterDescriptor
{
    /// <summary>
    /// Gets the stable adapter identity and implementation version.
    /// </summary>
    public required AdapterIdentity Identity { get; init; }

    /// <summary>
    /// Gets the source language understood by the adapter.
    /// </summary>
    public required AdapterSourceLanguage SourceLanguage { get; init; }

    /// <summary>
    /// Gets the semantic concern owned by the adapter.
    /// </summary>
    public required AdapterCategory Category { get; init; }

    /// <summary>
    /// Gets the supported Generation package versions.
    /// </summary>
    public GenerationVersionRange CompatibleGenerationVersions { get; init; } = GenerationVersionRange.Any;

    /// <summary>
    /// Gets the source-neutral host capabilities required before the adapter can execute.
    /// </summary>
    public ImmutableArray<AdapterHostCapability> RequiredHostCapabilities { get; init; } = [];

    /// <summary>
    /// Gets the source-neutral API capabilities that an applicable probe must prove before execution.
    /// </summary>
    public ImmutableArray<AdapterApiCapability> RequiredApiCapabilities { get; init; } = [];

    /// <summary>
    /// Gets the neutral fact families the adapter is allowed to emit.
    /// </summary>
    public ImmutableArray<GenerationFactCapability> EmittedFactCapabilities { get; init; } = [];
}
