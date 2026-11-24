// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_freezing_input_only_executions : given.method_declaration_execution
{
    AdapterContributionAdmissionResult _result;
    MethodDeclarationExecutionFact _original;
    MethodDeclarationExecutionFact _frozen;
    AdapterContributionAdmissionResult _unknownOrder;

    void Because()
    {
        var scope = new BackendScopeKey { Store = Subject("store"), EventModelName = "library" };
        _original = Execution(7) with { Definition = Execution(7).Definition with { SourceScope = scope, TargetScope = scope } };
        _result = AdmitExecution(_original);
        _frozen = _result.Snapshot.Facts.OfType<MethodDeclarationExecutionFact>().Single();
        _unknownOrder = AdmitExecution(Execution(null));
    }

    [Fact] void should_admit_without_artifact_usage_capabilities() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_the_definition() => ReferenceEquals(_original.Definition, _frozen.Definition).ShouldBeFalse();
    [Fact] void should_freeze_method_identity() => ReferenceEquals(_original.Definition.Method, _frozen.Definition.Method).ShouldBeFalse();
    [Fact] void should_freeze_declaration_identity() => ReferenceEquals(_original.Definition.Declaration, _frozen.Definition.Declaration).ShouldBeFalse();
    [Fact] void should_freeze_scope_selectors() => ReferenceEquals(_original.Definition.TargetScope, _frozen.Definition.TargetScope).ShouldBeFalse();
    [Fact] void should_preserve_scope_selectors() => _frozen.Definition.ShouldEqual(_original.Definition);
    [Fact] void should_preserve_the_proved_position() => _frozen.Definition.ExecutionOrder.ShouldEqual(7);
    [Fact] void should_allow_unknown_semantic_position() => _unknownOrder.IsAdmitted.ShouldBeTrue();
    [Fact] void should_not_replace_unknown_position_with_a_sort_key() => _unknownOrder.Snapshot.Facts.OfType<MethodDeclarationExecutionFact>().Single().Definition.ExecutionOrder.ShouldBeNull();
}
