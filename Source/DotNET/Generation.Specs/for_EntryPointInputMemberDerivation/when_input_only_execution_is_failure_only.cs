// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_input_only_execution_is_failure_only : given.input_only_execution
{
    GenerationDerivationSnapshot[] _results;

    void Because()
    {
        var support = Support() with { Definition = Support().Definition with { Candidate = InputExecution().Id } };
        _results =
        [
            Derive(InputOnlyFacts(support, Classification(MethodExecutionKind.FailureOnly))),
            Derive(InputOnlyFacts(support with { Definition = support.Definition with { Alternatives = [Alternative(enclosing: [Parent])] } }, Classification(MethodExecutionKind.FailureOnly, Parent)))
        ];
    }

    [Fact] void should_not_resurrect_failure_only_or_lexically_nested_executions() => _results.All(result => Members(result).Length == 0).ShouldBeTrue();
    [Fact] void should_keep_the_original_failure_only_warning() => _results.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == Reason().Code)).ShouldBeTrue();
}
