// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.given;

public class a_vogen_compilation : Specification
{
    static readonly IReadOnlyList<MetadataReference> _platformReferences =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator)
            .Where(path => !string.Equals(
                Path.GetFileNameWithoutExtension(path),
                typeof(global::Vogen.ValueObjectAttribute).Assembly.GetName().Name,
                StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Select(_ => MetadataReference.CreateFromFile(_))
    ];

    static readonly IReadOnlyList<MetadataReference> _references =
    [
        .. _platformReferences,
        MetadataReference.CreateFromFile(typeof(global::Vogen.ValueObjectAttribute).Assembly.Location)
    ];

    protected static CSharpCompilation CompilationFrom(string assemblyName, params SourceFile[] sources) =>
        CSharpCompilation.Create(
            assemblyName,
            sources.Select(_ => CSharpSyntaxTree.ParseText(
                _.Content,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
                _.Path)),
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    protected static CSharpCompilation CompilationFromVogenApiSubset(
        string assemblyName,
        string vogenApi,
        params SourceFile[] sources)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);
        var vogen = CSharpCompilation.Create(
            "Vogen.SharedTypes",
            [CSharpSyntaxTree.ParseText(vogenApi, parseOptions, "VogenApi.cs")],
            _platformReferences,
            compilationOptions);
        return CSharpCompilation.Create(
            assemblyName,
            sources.Select(source => CSharpSyntaxTree.ParseText(source.Content, parseOptions, source.Path)),
            [.. _platformReferences, vogen.ToMetadataReference()],
            compilationOptions);
    }

    protected static AdapterContribution Analyze(params DotNetProjectCompilation[] projects) =>
        new VogenConceptScreenplayAdapter().Analyze(new DotNetAnalysisContext(projects), new DotNetAdapterOptions());

    protected static DotNetProjectCompilation Project(
        string name,
        CSharpCompilation compilation,
        string sourceRoot = "/workspace",
        IEnumerable<SyntaxTree>? authoredSyntaxTrees = null,
        DotNetProjectSourceContext? sourceContext = null) => new()
        {
            Name = name,
            Compilation = compilation,
            SourceRoot = sourceRoot,
            SourceContext = sourceContext,
            AuthoredSyntaxTrees = (authoredSyntaxTrees ?? compilation.SyntaxTrees.Where(_ => !DotNetGeneratedSource.IsGenerated(_))).ToHashSet()
        };

    protected static DotNetProjectCompilation MappedProject(
        string name,
        string projectIdentity,
        CSharpCompilation compilation)
    {
        var authored = compilation.SyntaxTrees.Where(tree => !DotNetGeneratedSource.IsGenerated(tree)).ToArray();
        var sourceContext = DotNetSourcePaths.Create(
            projectIdentity,
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            authored.Select(tree => new DotNetSourceDocument
            {
                SyntaxTree = tree,
                ProjectRelativePath = Path.GetFileName(tree.FilePath),
                WorkspaceRelativePath = Path.GetFileName(tree.FilePath)
            }));
        return Project(name, compilation, authoredSyntaxTrees: authored, sourceContext: sourceContext);
    }

    protected static ArtifactFact ConceptNamed(AdapterContribution contribution, string name) =>
        contribution.Facts.OfType<ArtifactFact>().Single(_ => _.Definition.Name == name);

    protected static ConceptRepresentationFact RepresentationFor(
        AdapterContribution contribution,
        ArtifactFact concept) =>
        contribution.Facts.OfType<ConceptRepresentationFact>().Single(_ => _.Subject == concept.Subject);

    protected sealed record SourceFile(string Path, string Content);
}
