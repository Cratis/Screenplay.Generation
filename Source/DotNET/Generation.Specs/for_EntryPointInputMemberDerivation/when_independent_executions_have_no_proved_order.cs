// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_independent_executions_have_no_proved_order : given.input_members
{
    GenerationDerivationSnapshot _result;

    void Because()
    {
        var other = new SubjectId { Value = "project:other-declaration" };
        var usage = Execution(null) with { Id = new FactId { Value = "other-execution" }, Definition = Execution(null).Definition with { Declaration = other } };
        _result = Derive([.. InputFacts(Surface("limit", declaration: other), SurfaceType("limit", declaration: other), usage).Where(fact => fact is not MethodArtifactUsageFact || fact.Id == usage.Id), Execution(null)]);
    }

    [Fact] void should_not_choose_from_fact_id_order() => Members(_result).ShouldBeEmpty();
    [Fact] void should_report_the_uncertain_precedence() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidEntryPointInputTransfer && diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict).ShouldBeTrue();
}
