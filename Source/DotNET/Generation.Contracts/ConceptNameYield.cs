// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Allows a producer to withdraw its own concept declaration, but only on a same-name conflict.
/// </summary>
/// <remarks>
/// Co-owners of the same declaration, including its backend scope, report one diagnostic when its code,
/// severity, outcome, subject, source location, and message match. Facts and lineage retain the canonical
/// union of every contributor's local declaration, support facts, and evidence. Different declarations
/// or diagnostic payloads remain separate; producers must still use unique fact identities.
/// </remarks>
public sealed record ConceptNameYieldDefinition
{
    /// <summary>
    /// Gets the diagnostic with the Conflict or Unsupported outcome explaining why the producer omits this declaration on conflict.
    /// </summary>
    public required GenerationDiagnostic Diagnostic { get; init; }
}
