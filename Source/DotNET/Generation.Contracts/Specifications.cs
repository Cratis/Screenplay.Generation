// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines the authored phase of a neutral specification step.
/// </summary>
public enum SpecificationStepPhase
{
    /// <summary>
    /// The adapter could not determine a supported phase.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// State established before behavior is exercised.
    /// </summary>
    Given = 0,

    /// <summary>
    /// The behavior being exercised.
    /// </summary>
    When = 1,

    /// <summary>
    /// An observable expected outcome.
    /// </summary>
    Then = 2
}

/// <summary>
/// Defines the framework-neutral behavior represented by a specification step.
/// </summary>
public enum SpecificationStepKind
{
    /// <summary>
    /// The adapter could not determine a supported step kind.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// An event occurrence.
    /// </summary>
    Event = 0,

    /// <summary>
    /// A keyed read-model state.
    /// </summary>
    ReadModel = 1,

    /// <summary>
    /// A command invocation.
    /// </summary>
    Command = 2,

    /// <summary>
    /// A read behavior invocation and result.
    /// </summary>
    Read = 3,

    /// <summary>
    /// A rejected outcome.
    /// </summary>
    Error = 4
}

/// <summary>
/// Identifies one source specification scenario independently from its evidence.
/// </summary>
public sealed record SpecificationScenarioKey
{
    /// <summary>
    /// Gets the source-level scenario subject.
    /// </summary>
    public required SubjectId Scenario { get; init; }
}

/// <summary>
/// Identifies one ordered step in a source specification scenario.
/// </summary>
public sealed record SpecificationStepKey
{
    /// <summary>
    /// Gets the owning scenario.
    /// </summary>
    public required SpecificationScenarioKey Scenario { get; init; }

    /// <summary>
    /// Gets the zero-based authored step position.
    /// </summary>
    public required int Index { get; init; }
}

/// <summary>
/// Describes a source specification scenario independently from its evidence.
/// </summary>
public sealed record SpecificationScenarioDefinition
{
    /// <summary>
    /// Gets the stable scenario identity.
    /// </summary>
    public required SpecificationScenarioKey Key { get; init; }

    /// <summary>
    /// Gets the authored scenario name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the exact source artifact that owns the scenario and supplies its placement.
    /// </summary>
    public required ArtifactKey TargetArtifact { get; init; }

    /// <summary>
    /// Gets the steps in authored execution and assertion order.
    /// </summary>
    public IReadOnlyList<SpecificationStepKey> Steps { get; init; } = [];
}

/// <summary>
/// Describes one ordered source specification step independently from its evidence.
/// </summary>
public sealed record SpecificationStepDefinition
{
    /// <summary>
    /// Gets the stable ordered step identity.
    /// </summary>
    public required SpecificationStepKey Key { get; init; }

    /// <summary>
    /// Gets the authored specification phase.
    /// </summary>
    public required SpecificationStepPhase Phase { get; init; }

    /// <summary>
    /// Gets the framework-neutral step behavior.
    /// </summary>
    public required SpecificationStepKind Kind { get; init; }

    /// <summary>
    /// Gets the exact event, read model, command, or read artifact referenced by the step.
    /// </summary>
    public ArtifactKey? Artifact { get; init; }

    /// <summary>
    /// Gets the stable rejection code when the step expects one.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets the exact rejection message when the authored scenario asserts one.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the exact typed values attached to the step in authored order.
    /// </summary>
    public IReadOnlyList<SpecificationValueKey> Values { get; init; } = [];
}

/// <summary>
/// Asserts one source specification scenario with its evidence.
/// </summary>
public sealed record SpecificationScenarioFact : GenerationFact
{
    /// <summary>
    /// Gets the asserted scenario definition.
    /// </summary>
    public required SpecificationScenarioDefinition Definition { get; init; }
}

/// <summary>
/// Asserts one source specification step with its step-level evidence.
/// </summary>
public sealed record SpecificationStepFact : GenerationFact
{
    /// <summary>
    /// Gets the asserted step definition.
    /// </summary>
    public required SpecificationStepDefinition Definition { get; init; }
}
