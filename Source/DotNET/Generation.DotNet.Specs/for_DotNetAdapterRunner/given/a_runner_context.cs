// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner.given;

public class a_runner_context : DotNet.given.a_compilation
{
    protected static readonly DotNetAdapterOptions Options = new();

    protected static AdapterDescriptor Descriptor(
        string id,
        string version = "1.0.0",
        AdapterSourceLanguage language = AdapterSourceLanguage.SourceIndependent,
        AdapterCategory category = AdapterCategory.Concepts,
        ImmutableArray<AdapterHostCapability> hostCapabilities = default,
        ImmutableArray<AdapterApiCapability> apiCapabilities = default,
        ImmutableArray<GenerationFactCapability> factCapabilities = default,
        GenerationVersionRange? generationVersions = null) => new()
        {
            Identity = new AdapterIdentity { Id = id, Version = version },
            SourceLanguage = language,
            Category = category,
            CompatibleGenerationVersions = generationVersions ?? GenerationVersionRange.Any,
            RequiredHostCapabilities = hostCapabilities.IsDefault ? [] : hostCapabilities,
            RequiredApiCapabilities = apiCapabilities.IsDefault ? [] : apiCapabilities,
            EmittedFactCapabilities = factCapabilities.IsDefault ? [] : factCapabilities
        };

    protected static AdapterContribution EmptyContribution(string id, string version = "1.0.0") => new()
    {
        Adapter = new AdapterIdentity { Id = id, Version = version }
    };

    protected static AdapterContribution ArtifactContribution(
        AdapterIdentity identity,
        SourceRange? source = null,
        string? factId = null,
        List<GenerationFact>? facts = null)
    {
        facts ??= [];
        facts.Add(new ArtifactFact
        {
            Id = new FactId { Value = factId ?? $"{identity.Id}:artifact" },
            Subject = new SubjectId { Value = $"dotnet://Specs/{identity.Id}/Artifact" },
            Evidence = new Evidence
            {
                Adapter = identity,
                Strength = EvidenceStrength.Exact,
                Source = source
            },
            Definition = new ArtifactDefinition
            {
                Key = new ArtifactKey
                {
                    Subject = new SubjectId { Value = $"dotnet://Specs/{identity.Id}/Artifact" },
                    Kind = ArtifactKind.Command
                },
                Name = "Register"
            }
        });
        return new AdapterContribution { Adapter = identity, Facts = facts };
    }

    protected static DotNetProjectCompilation MappedProject(
        string projectIdentity,
        string name,
        string assemblyName,
        string path,
        string content,
        string displayPath = "Code.cs",
        bool authored = true)
    {
        var tree = CSharpSyntaxTree.ParseText(
            content,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            path);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [tree],
            CompilationFrom().References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var sourceContext = DotNetSourcePaths.Create(
            projectIdentity,
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = tree,
                    ProjectRelativePath = displayPath,
                    WorkspaceRelativePath = displayPath
                }
            ]);
        return new DotNetProjectCompilation
        {
            Name = name,
            Compilation = compilation,
            SourceContext = sourceContext,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.Where(_ => authored).ToHashSet()
        };
    }

    protected sealed class ModernAdapter(AdapterDescriptor descriptor) : IDescribedDotNetScreenplayAdapter
    {
        public AdapterDescriptor Descriptor
        {
            get
            {
                DescriptorCount++;
                return OnDescriptor?.Invoke() ?? descriptor;
            }
        }

        public AdapterProbeResult ProbeResult { get; set; } = new AdapterProbeApplicable();
        public AdapterContribution Contribution { get; set; } = EmptyContribution(descriptor.Identity.Id, descriptor.Identity.Version);
        public Func<AdapterDescriptor>? OnDescriptor { get; set; }
        public Func<DotNetAnalysisContext, AdapterProbeResult>? OnProbe { get; set; }
        public Func<DotNetAnalysisContext, DotNetAdapterOptions, AdapterContribution>? OnAnalyze { get; set; }
        public int DescriptorCount { get; private set; }
        public int ProbeCount { get; private set; }
        public int AnalyzeCount { get; private set; }

        public AdapterProbeResult Probe(DotNetAnalysisContext context)
        {
            ProbeCount++;
            return OnProbe?.Invoke(context) ?? ProbeResult;
        }

        public AdapterContribution Analyze(DotNetAnalysisContext context, DotNetAdapterOptions options)
        {
            AnalyzeCount++;
            return OnAnalyze?.Invoke(context, options) ?? Contribution;
        }
    }

    protected sealed class LegacyAdapter(AdapterIdentity identity) : IDotNetScreenplayAdapter
    {
        public AdapterIdentity Identity => identity;
        public bool IsApplicable { get; set; } = true;
        public AdapterContribution Contribution { get; set; } = EmptyContribution(identity.Id, identity.Version);
        public int CanAnalyzeCount { get; private set; }
        public int AnalyzeCount { get; private set; }

        public bool CanAnalyze(DotNetAnalysisContext context)
        {
            _ = context;
            CanAnalyzeCount++;
            return IsApplicable;
        }

        public AdapterContribution Analyze(DotNetAnalysisContext context, DotNetAdapterOptions options)
        {
            _ = context;
            _ = options;
            AnalyzeCount++;
            return Contribution;
        }
    }
}
