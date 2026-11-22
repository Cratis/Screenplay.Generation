// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_receiving_an_esm_refusal : given.a_generator
{
    GenerationDiagnostic _refusal = null!;
    GeneratedScreenplayDefinition[] _results = [];

    void Establish() => _refusal = EsmCanonicalReader.Read(RegisterProjectCorpus.V7.EsmBytes.AsSpan(), EsmSurface.ReverseRecovery).Diagnostics.Single();

    void Because()
    {
        var options = new ScreenplayGenerationOptions { Domain = "Projects" };
        _results =
        [
            Generator.Generate([Contribution() with { Diagnostics = [_refusal] }], options),
            Generator.Generate(Snapshot(Completed(Adapter, [], [_refusal])), options)
        ];
    }

    [Fact] void should_exercise_both_generator_overloads() => _results.Length.ShouldEqual(2);
    [Fact] void should_block_both_results() => _results.Any(result => result.IsSuccess).ShouldBeFalse();
    [Fact] void should_preserve_the_refusal_code() => _results.All(result => result.Diagnostics.Single().Code == _refusal.Code).ShouldBeTrue();
    [Fact] void should_preserve_the_error_severity() => _results.All(result => result.Diagnostics.Single().Severity == GenerationDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_preserve_the_unsupported_outcome() => _results.All(result => result.Diagnostics.Single().Outcome == GenerationDiagnosticOutcome.Unsupported).ShouldBeTrue();
    [Fact] void should_preserve_the_message_and_issue_link() => _results.All(result => result.Diagnostics.Single().Message == _refusal.Message && result.Diagnostics.Single().Message.Contains("https://github.com/Cratis/Screenplay.Generation/issues/71", StringComparison.Ordinal)).ShouldBeTrue();
}
