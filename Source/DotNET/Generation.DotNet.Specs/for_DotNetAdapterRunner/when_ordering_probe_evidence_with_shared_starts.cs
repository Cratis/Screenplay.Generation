// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_ordering_probe_evidence_with_shared_starts : given.a_runner_context
{
    string _forward = null!;
    string _reverse = null!;

    void Because()
    {
        var project = MappedProject(
            "project",
            "Project",
            "Project",
            "/checkout/Project/Code.cs",
            "public class Code;\n");
        var file = project.SourceContext!.Files.Values.Single();
        var shortRange = Range(file, 2);
        var longRange = Range(file, 7);
        _forward = Projection(Run(project, [Evidence(longRange), Evidence(shortRange)]));
        _reverse = Projection(Run(project, [Evidence(shortRange), Evidence(longRange)]));
    }

    [Fact] void should_include_end_coordinates_in_the_total_evidence_order() => _forward.ShouldEqual("1:1-1:2|1:1-1:7");
    [Fact] void should_keep_the_snapshot_identical_when_same_start_ranges_are_reversed() => _reverse.ShouldEqual(_forward);

    static AdapterRunSnapshot Run(
        DotNetProjectCompilation project,
        AdapterProbeEvidence[] evidence)
    {
        var adapter = new ModernAdapter(Descriptor("evidence-order"))
        {
            ProbeResult = new AdapterProbeApplicable { Evidence = [.. evidence] }
        };
        return DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(adapter)],
            new DotNetAnalysisContext([project]),
            Options);
    }

    static AdapterProbeEvidence Evidence(SourceRange source) => new()
    {
        Description = "The exact API is present",
        Source = source,
        Subject = new SubjectId { Value = "dotnet://Project/Code" }
    };

    static SourceRange Range(DotNetSourceFile file, int endColumn) => new()
    {
        Path = file.DisplayPath,
        FileIdentity = file.Identity,
        StartLine = 1,
        StartColumn = 1,
        EndLine = 1,
        EndColumn = endColumn
    };

    static string Projection(AdapterRunSnapshot snapshot) => string.Join(
        '|',
        snapshot.Adapters.Single().Probe.Evidence.Select(evidence =>
            $"{evidence.Source!.StartLine}:{evidence.Source.StartColumn}-{evidence.Source.EndLine}:{evidence.Source.EndColumn}"));
}
