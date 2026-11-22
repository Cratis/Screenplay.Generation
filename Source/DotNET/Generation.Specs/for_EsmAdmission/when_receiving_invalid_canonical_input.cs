// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Screenplay.Generation.for_EsmAdmission;

public class when_receiving_invalid_canonical_input : Specification
{
    byte[][] _inputs = [];
    EsmCanonicalReadResult[] _results = [];

    void Establish()
    {
        var vector = EsmGoldenVectors.Get(6);
        var source = Encoding.UTF8.GetString(vector.Bytes.AsSpan());
        var revision = vector.SemanticRevision.ToString();
        var wrongRevision = revision[..^1] + (revision[^1] == '0' ? '1' : '0');
        string[] invalid =
        [
            source[..^1],
            source.Insert(1, "\"unknown\":true,"),
            source.Insert(1, "\"schema\":\"cratis.screenplay.esm\","),
            source.Replace(revision, wrongRevision, StringComparison.Ordinal),
            source + "{}",
            source.Insert(1, " ")
        ];
        _inputs = [.. invalid.Select(Encoding.UTF8.GetBytes)];
    }

    void Because() => _results = [.. _inputs.Select(input => EsmCanonicalReader.Read(input, EsmSurface.Reading))];

    [Fact] void should_check_all_six_invalid_inputs() => _results.Length.ShouldEqual(6);
    [Fact] void should_refuse_every_input() => _results.All(result => result.Decision.Disposition == EsmDisposition.Refused).ShouldBeTrue();
    [Fact] void should_identify_invalid_canonical_input() => _results.All(result => result.Decision.Reason == EsmAdmissionReason.InvalidCanonicalInput).ShouldBeTrue();
    [Fact] void should_not_release_a_model() => _results.All(result => result.Model is null).ShouldBeTrue();
    [Fact] void should_not_admit_or_report_success() => _results.All(result => !result.IsAdmitted && !result.IsSuccess).ShouldBeTrue();
    [Fact] void should_use_the_stable_invalid_input_code() => _results.All(result => result.Diagnostics.Single().Code == "GEN0066").ShouldBeTrue();
    [Fact] void should_block_publication() => _results.All(result => result.Diagnostics.Single().Severity == GenerationDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_report_unsupported_input() => _results.All(result => result.Diagnostics.Single().Outcome == GenerationDiagnosticOutcome.Unsupported).ShouldBeTrue();
}
