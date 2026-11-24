// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_input_only_execution_is_for_another_occurrence : given.input_only_execution
{
    GenerationDerivationSnapshot[] _results;

    void Because()
    {
        var execution = InputExecution();
        var definitions = new[]
        {
            execution.Definition with { Method = Overload },
            execution.Definition with { Declaration = new SubjectId { Value = "dotnet://Other/Plans.FindCustomer``1" } },
            execution.Definition with { SourceScope = Scope("other") },
            execution.Definition with { TargetScope = Scope("other") }
        };
        _results = [.. definitions.Select(definition => Derive([.. InputOnlyFacts(Backend(Scope("other"))).Where(fact => fact is not MethodDeclarationExecutionFact), execution with { Subject = definition.Method, Definition = definition }]))];
    }

    [Fact] void should_not_fall_back_across_methods_declarations_or_backend_occurrences() => _results.All(result => Members(result).Length == 0).ShouldBeTrue();
}
