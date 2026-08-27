// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_callbacks_throw_under_reversed_input : given.a_runner_context
{
    string _forward = null!;
    string _reverse = null!;

    void Because()
    {
        var forward = Adapters("first secret /checkout/a", "second secret /checkout/b");
        var reverse = Adapters("different /private/c", "different /private/d");
        _forward = Projection(DotNetAdapterRunner.Run(
            forward.Select(DotNetAdapterRegistration.For),
            new DotNetAnalysisContext([]),
            Options));
        _reverse = Projection(DotNetAdapterRunner.Run(
            reverse.AsEnumerable().Reverse().Select(DotNetAdapterRegistration.For),
            new DotNetAnalysisContext([]),
            Options));
    }

    [Fact] void should_keep_exception_diagnostics_deterministic() => _reverse.ShouldEqual(_forward);
    [Fact] void should_include_only_exception_types_not_messages_or_paths() => _forward.ShouldEqual("analysis:Analyze:System.InvalidOperationException|probe:Probe:System.ArgumentException");

    static ModernAdapter[] Adapters(string analyzeMessage, string probeMessage)
    {
        var analysis = new ModernAdapter(Descriptor("analysis"))
        {
            OnAnalyze = (_, _) => throw new InvalidOperationException(analyzeMessage)
        };
        var probe = new ModernAdapter(Descriptor("probe"))
        {
            OnProbe = _ => throw new ArgumentException(probeMessage)
        };
        return [analysis, probe];
    }

    static string Projection(AdapterRunSnapshot snapshot) => string.Join(
        '|',
        snapshot.Adapters.Select(record =>
        {
            var message = record.Execution.Diagnostics.Single().Message;
            var operation = message.Split('\'')[3];
            var exceptionType = message.Split('\'')[5];
            return $"{record.Descriptor.Identity.Id}:{operation}:{exceptionType}";
        }));
}
