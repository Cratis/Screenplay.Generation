// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Represents the semantic facts and diagnostics contributed by one adapter analysis.
/// </summary>
public sealed record AdapterContribution
{
    /// <summary>
    /// Gets the current semantic fact schema version.
    /// </summary>
    public const string CurrentSchemaVersion = "1.0";

    /// <summary>
    /// Gets the semantic fact schema version used by this contribution.
    /// </summary>
    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets the adapter that produced the contribution.
    /// </summary>
    public required AdapterIdentity Adapter { get; init; }

    /// <summary>
    /// Gets the contributed semantic facts.
    /// </summary>
    public IReadOnlyList<GenerationFact> Facts { get; init; } = [];

    /// <summary>
    /// Gets diagnostics produced while discovering the facts.
    /// </summary>
    public IReadOnlyList<GenerationDiagnostic> Diagnostics { get; init; } = [];
}
