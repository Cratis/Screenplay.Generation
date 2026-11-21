// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_artifact_usage_mappings_conflict : given.an_artifact_usage
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts(), UsageDeclaration("other", Document with { Subject = new SubjectId { Value = "dotnet://Other/Models.Customer" } })]);

    [Fact] void should_not_fan_out_the_target() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_report_a_typed_conflict() => _result.Diagnostics.Single().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Conflict);
    [Fact] void should_link_both_target_mappings() => _result.Diagnostics.Single().Facts.Select(id => id.Value).ShouldContain("library:other");
}
