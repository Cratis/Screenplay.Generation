// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_mixed_execution_observations_disagree : given.input_only_execution
{
    GenerationDerivationSnapshot[] _results;
    GenerationDerivationSnapshot[] _reversed;

    void Because()
    {
        var observations = new[] { Execution(3), Execution(null) };
        _results = [.. observations.Select(observation => Derive(InputOnlyFacts(observation)))];
        _reversed = [.. observations.Select(observation => Derive([.. InputOnlyFacts(observation).Reverse()]))];
    }

    [Fact] void should_not_choose_between_conflicting_or_missing_order_proofs() => _results.All(result => Members(result).Length == 0).ShouldBeTrue();
    [Fact] void should_report_located_conflicts() => _results.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidEntryPointInputTransfer && diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict && diagnostic.Source is not null)).ShouldBeTrue();
    [Fact] void should_retain_both_execution_observations_in_the_diagnostic() => _results.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Facts.Contains(InputExecution().Id) && diagnostic.Facts.Contains(Execution().Id))).ShouldBeTrue();
    [Fact] void should_preserve_diagnostics_under_permutation() => Projection(_results).ShouldEqual(Projection(_reversed));
}
