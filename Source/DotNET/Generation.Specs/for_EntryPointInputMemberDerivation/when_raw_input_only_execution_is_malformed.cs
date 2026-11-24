// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_raw_input_only_execution_is_malformed : given.input_only_execution
{
    GenerationDerivationSnapshot[] _results;

    void Because()
    {
        var execution = InputExecution();
        var invalid = new[]
        {
            execution with { Definition = null! },
            execution with { Definition = execution.Definition with { Method = null! } },
            execution with { Definition = execution.Definition with { Declaration = null! } },
            execution with { Evidence = execution.Evidence with { Strength = EvidenceStrength.Heuristic } },
            execution with { Subject = Overload },
            execution with { Definition = execution.Definition with { ExecutionOrder = -1 } },
            execution with { Definition = execution.Definition with { Declaration = new SubjectId { Value = string.Empty } } }
        };
        _results = [.. invalid.Select(fact => Derive([.. InputOnlyFacts().Where(candidate => candidate is not MethodDeclarationExecutionFact), fact]))];
    }

    [Fact] void should_not_transfer_malformed_observations() => _results.All(result => Members(result).Length == 0).ShouldBeTrue();
    [Fact] void should_diagnose_the_exact_malformed_fact() => _results.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidEntryPointInputTransfer && diagnostic.Facts.Contains(InputExecution().Id) && diagnostic.Source is not null)).ShouldBeTrue();
}
