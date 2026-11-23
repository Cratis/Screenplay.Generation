// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_co_owned_concept_yield_payloads_differ : given.co_owned_yielding_concepts
{
    GeneratedScreenplayDefinition _code = null!;
    GeneratedScreenplayDefinition _severity = null!;
    GeneratedScreenplayDefinition _outcome = null!;
    GeneratedScreenplayDefinition _message = null!;
    GeneratedScreenplayDefinition _location = null!;
    GeneratedScreenplayDefinition _subject = null!;

    void Because()
    {
        var first = CoOwner("first");
        _code = GenerateCoOwners(first, CoOwner("second", diagnostic => diagnostic with { Code = $"{diagnostic.Code}.Other" }));
        _severity = GenerateCoOwners(first, CoOwner("second", diagnostic => diagnostic with { Severity = GenerationDiagnosticSeverity.Information }));
        _outcome = GenerateCoOwners(first, CoOwner("second", diagnostic => diagnostic with { Outcome = GenerationDiagnosticOutcome.Unsupported }));
        _message = GenerateCoOwners(first, CoOwner("second", diagnostic => diagnostic with { Message = "Another reason to omit this concept" }));
        _location = GenerateCoOwners(first, CoOwner("second", diagnostic => diagnostic with { Source = diagnostic.Source! with { StartColumn = 2 } }));
        _subject = GenerateCoOwners(first, Candidate("another-enum", true, producer: new AdapterIdentity { Id = "second", Version = "1.0.0" }));
    }

    [Fact] void should_not_merge_different_codes() => _code.Diagnostics.Count(diagnostic => diagnostic.Code.StartsWith("example.EnumConceptNameConflict", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_not_merge_different_severities() => _severity.Diagnostics.Count(IsWarning).ShouldEqual(2);
    [Fact] void should_not_merge_different_outcomes() => _outcome.Diagnostics.Count(IsWarning).ShouldEqual(2);
    [Fact] void should_not_merge_different_messages() => _message.Diagnostics.Count(IsWarning).ShouldEqual(2);
    [Fact] void should_not_merge_different_locations() => _location.Diagnostics.Count(IsWarning).ShouldEqual(2);
    [Fact] void should_not_merge_different_subjects() => _subject.Diagnostics.Count(IsWarning).ShouldEqual(2);
    [Fact] void should_keep_each_payloads_local_support() => _message.Diagnostics.Where(IsWarning).Select(diagnostic => diagnostic.Facts.Length).ShouldEqual([2, 2]);
}
