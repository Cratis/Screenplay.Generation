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

/// <summary>
/// Provides the shared building blocks of the Screenplay lowering steps.
/// </summary>
internal static class LoweringSupport
{
    public static SourceLocation Generated { get; } = SourceLocation.Start;

    public static GenerationDiagnostic Warning(string code, GenerationDiagnosticOutcome outcome, string message) => new()
    {
        Code = code,
        Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = outcome,
        Message = message
    };

    public static SourceRange? FirstSource(IEnumerable<Evidence> evidence) =>
        evidence
            .Where(_ => _.Source is not null)
            .OrderBy(Canonical.Evidence, StringComparer.Ordinal)
            .Select(_ => _.Source)
            .FirstOrDefault();

    public static bool IsKnownArtifactKind(ArtifactKind kind) => Enum.IsDefined(kind) && kind != ArtifactKind.Unknown;

    public static bool CanLower(ArtifactKind kind) => kind is ArtifactKind.Command or ArtifactKind.Event or ArtifactKind.Query or ArtifactKind.ReadModel or ArtifactKind.Reducer;

    public static GenerationDiagnosticOutcome OutcomeFor<TEnum>(TEnum value, TEnum unknown)
        where TEnum : struct, Enum =>
        EqualityComparer<TEnum>.Default.Equals(value, unknown)
            ? GenerationDiagnosticOutcome.Unknown
            : GenerationDiagnosticOutcome.Unsupported;

    public static FileReferenceSyntax? FileFrom(string? path) =>
        path is null ? null : new FileReferenceSyntax(path.Replace('\\', '/'), Generated);
}
