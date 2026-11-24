// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_input_only_execution_is_malformed : given.method_declaration_execution
{
    AdapterContributionAdmissionResult[] _results;

    void Because()
    {
        var execution = Execution();
        var invalid = new[]
        {
            execution with { Definition = null! },
            Execution(-1),
            execution with { Definition = execution.Definition with { Method = ExternalSubject } },
            execution with { Definition = execution.Definition with { Method = new SubjectId { Value = string.Empty } } },
            execution with { Definition = execution.Definition with { Declaration = new SubjectId { Value = string.Empty } } },
            execution with { Definition = execution.Definition with { TargetScope = new BackendScopeKey { Store = new SubjectId { Value = string.Empty } } } }
        };
        _results = [.. invalid.Select(AdmitExecution)];
    }

    [Fact] void should_reject_missing_definition_invalid_owner_identity_order_and_scope() => _results.All(result => !result.IsAdmitted).ShouldBeTrue();
    [Fact] void should_locate_the_invalid_fact() => _results.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Fact == Execution().Id)).ShouldBeTrue();
    [Fact] void should_reject_negative_semantic_order_explicitly() => _results[1].Diagnostics.Any(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.InvalidDeclarationOrder).ShouldBeTrue();
}
