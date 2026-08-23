// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_conflicting_artifacts : given.facts
{
    ResolvedApplicationGraph _result = null!;

    void Because() => _result = new GenerationResolver().Resolve(
    [
        Contribution(FirstAdapter, Fact("first-event", FirstAdapter)),
        Contribution(SecondAdapter, Fact("renamed-event", SecondAdapter, EventDefinition("AccountWasOpened")))
    ]);

    [Fact] void should_retain_both_definitions() => _result.Artifacts.Single().Variants.Count.ShouldEqual(2);
    [Fact] void should_mark_the_artifact_as_conflicted() => _result.Artifacts.Single().IsConflicted.ShouldBeTrue();
    [Fact] void should_report_the_conflict() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.ConflictingArtifact);
    [Fact] void should_type_the_conflict_outcome() => _result.Diagnostics.Single().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Conflict);
    [Fact] void should_report_the_subject() => _result.Diagnostics.Single().Subject.ShouldEqual(EventSubject);
}
