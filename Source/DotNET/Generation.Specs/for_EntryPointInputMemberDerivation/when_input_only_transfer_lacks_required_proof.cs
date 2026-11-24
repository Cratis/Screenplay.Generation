// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_input_only_transfer_lacks_required_proof : given.input_only_execution
{
    GenerationDerivationSnapshot[] _results;

    void Because() => _results =
    [
        Derive([.. InputOnlyFacts().Where(fact => fact is not EntryPointInputTransferFact)]),
        Derive([.. InputOnlyFacts().Where(fact => fact is not MethodDeclarationExecutionFact)]),
        Derive([.. InputOnlyFacts().Where(fact => fact is not EntryPointFact)])
    ];

    [Fact] void should_require_intent_execution_and_exact_entry_point() => _results.All(result => Members(result).Length == 0).ShouldBeTrue();
    [Fact] void should_diagnose_the_missing_entry_point() => _results[2].Diagnostics.Any(diagnostic => diagnostic.Severity == GenerationDiagnosticSeverity.Error).ShouldBeTrue();
}
