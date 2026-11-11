// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterProbeAdmission.given;

public class a_probe_admission_context : for_DotNetAdapterRunner.given.a_runner_context
{
    protected const string AdapterId = "probe-adapter";

    protected static readonly string Rejected = Describe(DotNetAdapterGenerationDiagnosticCodes.ProbeRejected, "Adapter 'probe-adapter' returned a malformed structured probe result");

    protected static AdapterRunRecord Probe(
        AdapterProbeResult? probe,
        DotNetProjectCompilation? project = null,
        params string[] requiredCapabilities) => Run(probe, project, requiredCapabilities).Record;

    protected static (AdapterRunRecord Record, ProbingAdapter Adapter) Run(
        AdapterProbeResult? probe,
        DotNetProjectCompilation? project = null,
        params string[] requiredCapabilities)
    {
        var adapter = new ProbingAdapter(
            Descriptor(
                AdapterId,
                apiCapabilities: [.. requiredCapabilities.Select(id => new AdapterApiCapability { Id = id })]),
            probe);
        var snapshot = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(adapter)],
            new DotNetAnalysisContext(project is null ? [] : [project]),
            Options);
        return (snapshot.Adapters.Single(), adapter);
    }

    protected static DotNetProjectCompilation Project() => MappedProject(
        "project",
        "Project",
        "Project",
        "/checkout/Project/Code.cs",
        "public class Code;\n");

    protected static SourceRange RangeIn(DotNetProjectCompilation project, int startColumn, int endColumn)
    {
        var file = project.SourceContext.Files.Values.Single();
        return new SourceRange
        {
            Path = file.DisplayPath,
            FileIdentity = file.Identity,
            StartLine = 1,
            StartColumn = startColumn,
            EndLine = 1,
            EndColumn = endColumn
        };
    }

    protected static AdapterProbeEvidence Evidence(
        string description,
        string? capability = null,
        string? subject = null,
        SourceRange? source = null) => new()
        {
            Description = description,
            ApiCapability = capability is null ? null : new AdapterApiCapability { Id = capability },
            Subject = subject is null ? null : new SubjectId { Value = subject },
            Source = source
        };

    protected static AdapterProbeApplicable Applicable(params AdapterProbeEvidence[] evidence) => new() { Evidence = [.. evidence] };

    protected static string Describe(string code, string message) => $"{code}|{message}";

    protected static string Diagnostics(AdapterProbeResult probe) => string.Join(
        ';',
        ((AdapterProbeBlocked)probe).Diagnostics.Select(diagnostic => Describe(diagnostic.Code, diagnostic.Message)));

    protected static string Describe(AdapterProbeEvidence item) =>
        $"{item.Description}|{item.ApiCapability?.Id}|{item.Subject?.Value}|{item.Source?.StartColumn}-{item.Source?.EndColumn}";

    protected sealed class ProbingAdapter(AdapterDescriptor descriptor, AdapterProbeResult? probe) : IDescribedDotNetScreenplayAdapter
    {
        public AdapterDescriptor Descriptor => descriptor;
        public int AnalyzeCount { get; private set; }

        public AdapterProbeResult Probe(DotNetAnalysisContext context) => probe!;

        public AdapterContribution Analyze(DotNetAnalysisContext context, DotNetAdapterOptions options)
        {
            AnalyzeCount++;
            return EmptyContribution(descriptor.Identity.Id, descriptor.Identity.Version);
        }
    }

    protected static string Outcome(AdapterProbeEvidence item)
    {
        var probe = Probe(Applicable(item)).Probe;
        return probe is AdapterProbeApplicable ? "admitted" : Diagnostics(probe);
    }
}
