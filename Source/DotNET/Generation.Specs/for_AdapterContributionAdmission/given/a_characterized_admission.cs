// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.given;

public class a_characterized_admission : a_contribution
{
    static readonly JsonSerializerOptions _snapshotOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected static string[] Render(IEnumerable<(string Label, AdapterContributionAdmissionResult Result)> cases) =>
    [
        .. cases.SelectMany(entry => entry.Result.IsAdmitted
            ? [$"{entry.Label} => admitted"]
            : entry.Result.Diagnostics.Select(diagnostic =>
                $"{entry.Label} => {diagnostic.Code}|{diagnostic.Path}|{diagnostic.Message}|{diagnostic.Fact?.Value}|{diagnostic.Subject?.Value}|{SourceText(diagnostic.Source)}"))
    ];

    protected static string[] RenderCompact(IEnumerable<(string Label, AdapterContributionAdmissionResult Result)> cases) =>
    [
        .. cases.SelectMany(entry => entry.Result.IsAdmitted
            ? [$"{entry.Label} => admitted"]
            : entry.Result.Diagnostics.Select(diagnostic => $"{entry.Label} => {diagnostic.Code}|{diagnostic.Path.Replace("Contribution.Facts[atomic:", "[", StringComparison.Ordinal).Replace(".Definition", ".D", StringComparison.Ordinal)}"))
    ];

    protected static string[] RenderSource(IEnumerable<(string Label, AdapterContributionAdmissionResult Result)> cases) =>
    [
        .. cases.SelectMany(entry => entry.Result.IsAdmitted
            ? [$"{entry.Label} => admitted"]
            : entry.Result.Diagnostics.Select(diagnostic =>
                $"{entry.Label} => {diagnostic.Code}|{diagnostic.Path.Replace("Contribution.Facts[atomic:", "[", StringComparison.Ordinal)}|{(diagnostic.Message.EndsWith(PortableRangeMessage, StringComparison.Ordinal) ? "(portable-range)" : diagnostic.Message)}|{diagnostic.Subject?.Value}|{SourceText(diagnostic.Source).Replace("Accounts/Accounts/Register.cs/", "", StringComparison.Ordinal)}"))
    ];

    protected const string PortableRangeMessage = " must identify a normalized portable relative path without rooted, backslash, empty, or dot segments and an ordered positive 1-based range";

    protected static AdapterContributionAdmissionResult AdmitFacts(params GenerationFact[] facts) =>
        Admit(contribution: Contribution(facts));

    protected static string SnapshotLines(AdapterContributionSnapshot snapshot)
    {
        var evidence = JsonSerializer.Serialize(snapshot.Facts[0].Evidence, _snapshotOptions);
        return string.Join(
            '\n',
            [
                $"Descriptor {JsonSerializer.Serialize(snapshot.Descriptor, _snapshotOptions)}",
                $"Evidence {evidence}",
                .. snapshot.Facts.Select(fact => $"{fact.GetType().Name} {JsonSerializer.Serialize((object)fact, _snapshotOptions).Replace(evidence, "<evidence>", StringComparison.Ordinal)}"),
                .. snapshot.Diagnostics.Select(diagnostic => $"Diagnostic {JsonSerializer.Serialize(diagnostic, _snapshotOptions)}")
            ]);
    }

    static string SourceText(SourceRange? source) => source is null
        ? string.Empty
        : $"{source.FileIdentity?.Project}/{source.FileIdentity?.Path}/{source.Path}@{source.StartLine}:{source.StartColumn}-{source.EndLine}:{source.EndColumn}";
}
