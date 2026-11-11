// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Collects the diagnostics and fact coverage produced while lowering a resolved graph.
/// </summary>
internal sealed class LoweringSink
{
    public List<GenerationDiagnostic> Diagnostics { get; } = [];

    public ScreenplayLoweringCoverageBuilder Coverage { get; } = new();

    /// <summary>
    /// Reports a diagnostic and marks the facts identified by the semantic keys as omitted because of it.
    /// </summary>
    /// <param name="diagnostic">The diagnostic explaining the omission.</param>
    /// <param name="keys">The semantic keys of the omitted facts.</param>
    public void Omit(GenerationDiagnostic diagnostic, params string[] keys)
    {
        Diagnostics.Add(diagnostic);
        foreach (var key in keys)
        {
            Coverage.Omitted(key, diagnostic);
        }
    }
}
