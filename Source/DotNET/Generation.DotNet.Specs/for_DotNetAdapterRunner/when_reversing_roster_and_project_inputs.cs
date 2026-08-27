// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_reversing_roster_and_project_inputs : given.a_runner_context
{
    string _forwardCallbacks = null!;
    string _reverseCallbacks = null!;
    string _forwardSnapshot = null!;
    string _reverseSnapshot = null!;

    void Because()
    {
        var first = MappedProject("project-a", "Z.Project", "Shared", "/checkout/a/Code.cs", "public class A;");
        var second = MappedProject("project-b", "A.Project", "Shared", "/checkout/b/Code.cs", "public class B;");
        var forwardLog = new List<string>();
        var reverseLog = new List<string>();
        var forwardAdapters = Adapters(forwardLog);
        var reverseAdapters = Adapters(reverseLog);
        var forward = DotNetAdapterRunner.Run(
            forwardAdapters.Select(DotNetAdapterRegistration.For),
            new DotNetAnalysisContext([first, second]),
            Options);
        var reverse = DotNetAdapterRunner.Run(
            reverseAdapters.AsEnumerable().Reverse().Select(DotNetAdapterRegistration.For),
            new DotNetAnalysisContext([second, first]),
            Options);
        _forwardCallbacks = string.Join('|', forwardLog);
        _reverseCallbacks = string.Join('|', reverseLog);
        _forwardSnapshot = SnapshotProjection(forward);
        _reverseSnapshot = SnapshotProjection(reverse);
    }

    [Fact] void should_keep_callback_order_equivalent() => _reverseCallbacks.ShouldEqual(_forwardCallbacks);
    [Fact] void should_order_adapters_by_identity() => _forwardCallbacks.StartsWith("adapter-a:Probe", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_order_projects_by_stable_project_identity() => _forwardCallbacks.ShouldContain("project-a,project-b");
    [Fact] void should_keep_snapshot_content_equivalent() => _reverseSnapshot.ShouldEqual(_forwardSnapshot);

    static ModernAdapter[] Adapters(List<string> callbacks) =>
    [
        Adapter("adapter-z", callbacks),
        Adapter("adapter-a", callbacks)
    ];

    static ModernAdapter Adapter(string id, List<string> callbacks)
    {
        return new ModernAdapter(Descriptor(id))
        {
            OnProbe = context =>
            {
                callbacks.Add($"{id}:Probe:{Projects(context)}");
                return new AdapterProbeApplicable();
            },
            OnAnalyze = (context, _) =>
            {
                callbacks.Add($"{id}:Analyze:{Projects(context)}");
                return EmptyContribution(id);
            }
        };
    }

    static string Projects(DotNetAnalysisContext context) =>
        string.Join(',', context.Projects.Select(project => project.SourceContext!.ProjectIdentity));

    static string SnapshotProjection(AdapterRunSnapshot snapshot) => string.Join(
        '|',
        snapshot.Adapters.Select(record => $"{record.Descriptor.Identity.Id}@{record.Descriptor.Identity.Version}:{record.Disposition}:{record.Considered}:{record.Probed}:{record.Executed}"));
}
