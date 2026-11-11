// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_contribution_admission_throws : given.a_runner_context
{
    ModernAdapter _throwing = null!;
    ModernAdapter _following = null!;
    AdapterRunSnapshot _snapshot = null!;

    void Establish()
    {
        var identity = new AdapterIdentity { Id = "throwing", Version = "1.0.0" };
        _throwing = new(Descriptor("throwing"))
        {
            Contribution = new AdapterContribution { Adapter = identity, Facts = new ThrowingFacts() }
        };
        _following = new(Descriptor("following"));
    }

    void Because() => _snapshot = DotNetAdapterRunner.Run(
        [
            DotNetAdapterRegistration.For(_throwing),
            DotNetAdapterRegistration.For(_following)
        ],
        new DotNetAnalysisContext([]),
        Options);

    AdapterRunRecord Record(string id) => _snapshot.Adapters.Single(record => record.Descriptor.Identity.Id == id);

    [Fact] void should_fail_execution_of_the_throwing_adapter() => Record("throwing").Disposition.ShouldEqual(AdapterRunDisposition.ExecutionFailed);
    [Fact] void should_report_the_contribution_admission_operation() => Record("throwing").Execution.Diagnostics.Single().Message.Split('\'')[3].ShouldEqual("ContributionAdmission");
    [Fact] void should_analyze_the_throwing_adapter_exactly_once() => _throwing.AnalyzeCount.ShouldEqual(1);
    [Fact] void should_continue_to_the_following_adapter() => _following.AnalyzeCount.ShouldEqual(1);
    [Fact] void should_admit_the_following_adapter() => Record("following").Disposition.ShouldEqual(AdapterRunDisposition.Admitted);

    sealed class ThrowingFacts : IReadOnlyList<GenerationFact>
    {
        public int Count => throw new InvalidOperationException("facts enumeration failed");

        public GenerationFact this[int index] => throw new InvalidOperationException("facts enumeration failed");

        public IEnumerator<GenerationFact> GetEnumerator() => throw new InvalidOperationException("facts enumeration failed");

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
