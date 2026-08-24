// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Represents one atomically admitted typed specification value.
/// </summary>
public sealed record AdmittedSpecificationValue
{
    /// <summary>
    /// Gets the exact value definition.
    /// </summary>
    public required SpecificationValueDefinition Definition { get; init; }

    /// <summary>
    /// Gets the ordered evidence establishing the value.
    /// </summary>
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];

    /// <summary>
    /// Gets admitted ordered collection items or composite members.
    /// </summary>
    public IReadOnlyList<AdmittedSpecificationValue> Children { get; init; } = [];
}

/// <summary>
/// Represents one atomically admitted specification step.
/// </summary>
public sealed record AdmittedSpecificationStep
{
    /// <summary>
    /// Gets the exact step definition.
    /// </summary>
    public required SpecificationStepDefinition Definition { get; init; }

    /// <summary>
    /// Gets the ordered step-level evidence.
    /// </summary>
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];

    /// <summary>
    /// Gets the admitted typed values in authored order.
    /// </summary>
    public IReadOnlyList<AdmittedSpecificationValue> Values { get; init; } = [];
}

/// <summary>
/// Represents one complete specification scenario admitted as a single unit.
/// </summary>
public sealed record AdmittedSpecificationScenario
{
    /// <summary>
    /// Gets the exact scenario definition.
    /// </summary>
    public required SpecificationScenarioDefinition Definition { get; init; }

    /// <summary>
    /// Gets the exact resolved source placement of the owning target artifact.
    /// </summary>
    public required ArtifactPlacement Placement { get; init; }

    /// <summary>
    /// Gets the ordered scenario-level evidence.
    /// </summary>
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];

    /// <summary>
    /// Gets every admitted step in authored order.
    /// </summary>
    public IReadOnlyList<AdmittedSpecificationStep> Steps { get; init; } = [];
}
