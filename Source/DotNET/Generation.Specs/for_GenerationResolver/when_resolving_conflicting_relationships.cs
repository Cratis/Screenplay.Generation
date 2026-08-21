// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_conflicting_relationships : given.facts
{
    ResolvedApplicationGraph _result = null!;

    void Because() => _result = new GenerationResolver().Resolve(
    [
        Contribution(FirstAdapter, Relationship("first-production", FirstAdapter, "accountId")),
        Contribution(SecondAdapter, Relationship("second-production", SecondAdapter, "generatedId"))
    ]);

    [Fact] void should_retain_both_definitions() => _result.Relationships.Single().Definitions.Count.ShouldEqual(2);
    [Fact] void should_mark_the_relationship_as_conflicted() => _result.Relationships.Single().IsConflicted.ShouldBeTrue();
    [Fact] void should_report_the_conflict() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.ConflictingRelationship);
}
