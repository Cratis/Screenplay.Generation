// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

sealed class AdapterContributionAdmissionContext
{
    readonly List<AdapterContributionAdmissionDiagnostic> _diagnostics = [];

    public bool HasDiagnostics => _diagnostics.Count > 0;

    public void Missing(string path) => Add(
        AdapterContributionAdmissionDiagnosticCode.MissingRequiredValue,
        path,
        $"{path} is required");

    public void NullCollection(string path) => Add(
        AdapterContributionAdmissionDiagnosticCode.NullRequiredCollection,
        path,
        $"{path} must not be null");

    public void Add(
        AdapterContributionAdmissionDiagnosticCode code,
        string path,
        string message,
        FactId? fact = null,
        SubjectId? subject = null,
        SourceRange? source = null) =>
        _diagnostics.Add(new AdapterContributionAdmissionDiagnostic
        {
            Code = code,
            Path = path,
            Message = message,
            Fact = fact is null ? null : new FactId { Value = fact.Value ?? string.Empty },
            Subject = subject is null ? null : new SubjectId { Value = subject.Value ?? string.Empty },
            Source = source is null
                ? null
                : new SourceRange
                {
                    Path = source.Path,
                    FileIdentity = source.FileIdentity is null
                        ? null
                        : new SourceFileIdentity
                        {
                            Project = source.FileIdentity.Project,
                            Path = source.FileIdentity.Path
                        },
                    StartLine = source.StartLine,
                    StartColumn = source.StartColumn,
                    EndLine = source.EndLine,
                    EndColumn = source.EndColumn
                }
        });

    public void Enum<T>(T value, T unknown, string path, FactId? fact = null, SubjectId? subject = null)
        where T : struct, Enum
    {
        if (EqualityComparer<T>.Default.Equals(value, unknown))
        {
            Add(
                AdapterContributionAdmissionDiagnosticCode.UnknownEnumValue,
                path,
                $"{path} must not use {typeof(T).Name}.{unknown}",
                fact,
                subject);
        }
        else if (!System.Enum.IsDefined(value))
        {
            Add(
                AdapterContributionAdmissionDiagnosticCode.UndefinedEnumValue,
                path,
                $"{path} contains undefined {typeof(T).Name} value '{Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)}'",
                fact,
                subject);
        }
    }

    public ImmutableArray<AdapterContributionAdmissionDiagnostic> Diagnostics() =>
    [
        .. _diagnostics
            .OrderBy(diagnostic => diagnostic.Code)
            .ThenBy(diagnostic => diagnostic.Path, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Fact?.Value, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Subject?.Value, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Source is null ? 0 : 1)
            .ThenBy(diagnostic => diagnostic.Source?.FileIdentity?.Project, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Source?.FileIdentity?.Path, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Source?.Path, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Source?.StartLine)
            .ThenBy(diagnostic => diagnostic.Source?.StartColumn)
            .ThenBy(diagnostic => diagnostic.Source?.EndLine)
            .ThenBy(diagnostic => diagnostic.Source?.EndColumn)
    ];
}
