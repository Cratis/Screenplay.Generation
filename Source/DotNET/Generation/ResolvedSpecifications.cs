// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Represents one distinct specification scenario definition and its ordered evidence.
/// </summary>
public sealed record ResolvedSpecificationScenarioVariant
{
    /// <summary>
    /// Gets the asserted scenario definition.
    /// </summary>
    public required SpecificationScenarioDefinition Definition { get; init; }

    /// <summary>
    /// Gets the ordered evidence supporting the definition.
    /// </summary>
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];
}

/// <summary>
/// Represents every distinct definition asserted for one specification scenario.
/// </summary>
public sealed record ResolvedSpecificationScenario
{
    /// <summary>
    /// Gets the scenario identity.
    /// </summary>
    public required SpecificationScenarioKey Key { get; init; }

    /// <summary>
    /// Gets the distinct definitions in deterministic order.
    /// </summary>
    public IReadOnlyList<ResolvedSpecificationScenarioVariant> Variants { get; init; } = [];

    /// <summary>
    /// Gets whether incompatible scenario definitions were asserted.
    /// </summary>
    public bool IsConflicted => Variants.Count > 1;
}

/// <summary>
/// Represents one distinct ordered specification step definition and its evidence.
/// </summary>
public sealed record ResolvedSpecificationStepVariant
{
    /// <summary>
    /// Gets the asserted step definition.
    /// </summary>
    public required SpecificationStepDefinition Definition { get; init; }

    /// <summary>
    /// Gets the ordered step-level evidence.
    /// </summary>
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];
}

/// <summary>
/// Represents every distinct definition asserted for one ordered specification step.
/// </summary>
public sealed record ResolvedSpecificationStep
{
    /// <summary>
    /// Gets the ordered step identity.
    /// </summary>
    public required SpecificationStepKey Key { get; init; }

    /// <summary>
    /// Gets the distinct definitions in deterministic order.
    /// </summary>
    public IReadOnlyList<ResolvedSpecificationStepVariant> Variants { get; init; } = [];

    /// <summary>
    /// Gets whether incompatible step definitions were asserted.
    /// </summary>
    public bool IsConflicted => Variants.Count > 1;
}

/// <summary>
/// Represents one distinct specification value definition and its evidence.
/// </summary>
public sealed record ResolvedSpecificationValueVariant
{
    /// <summary>
    /// Gets the asserted value definition.
    /// </summary>
    public required SpecificationValueDefinition Definition { get; init; }

    /// <summary>
    /// Gets the ordered value-level evidence.
    /// </summary>
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];
}

/// <summary>
/// Represents every distinct definition asserted for one specification value path.
/// </summary>
public sealed record ResolvedSpecificationValue
{
    /// <summary>
    /// Gets the value identity.
    /// </summary>
    public required SpecificationValueKey Key { get; init; }

    /// <summary>
    /// Gets the distinct definitions in deterministic order.
    /// </summary>
    public IReadOnlyList<ResolvedSpecificationValueVariant> Variants { get; init; } = [];

    /// <summary>
    /// Gets whether incompatible value definitions were asserted.
    /// </summary>
    public bool IsConflicted => Variants.Count > 1;
}
