// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.given;

public class a_vogen_compilation : Specification
{
    static readonly IReadOnlyList<MetadataReference> _references =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(global::Vogen.ValueObjectAttribute).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(_ => MetadataReference.CreateFromFile(_))
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

    protected static AdapterContribution Analyze(params DotNetProjectCompilation[] projects) =>
        new VogenConceptScreenplayAdapter().Analyze(new DotNetAnalysisContext(projects), new DotNetAdapterOptions());

    protected static DotNetProjectCompilation Project(
        string name,
        CSharpCompilation compilation,
        string sourceRoot = "/workspace") => new()
    {
        Name = name,
        Compilation = compilation,
        SourceRoot = sourceRoot
    };

    protected static ArtifactFact ConceptNamed(AdapterContribution contribution, string name) =>
        contribution.Facts.OfType<ArtifactFact>().Single(_ => _.Definition.Name == name);

    protected static ConceptRepresentationFact RepresentationFor(
        AdapterContribution contribution,
        ArtifactFact concept) =>
        contribution.Facts.OfType<ConceptRepresentationFact>().Single(_ => _.Subject == concept.Subject);

    protected sealed record SourceFile(string Path, string Content);
}
