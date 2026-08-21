// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.given;

public class a_compilation : Specification
{
    static readonly IReadOnlyList<MetadataReference> _references =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(_ => MetadataReference.CreateFromFile(_))
    ];

    protected static CSharpCompilation CompilationFrom(params SourceFile[] sources) =>
        CSharpCompilation.Create(
            "Banking",
            sources.Select(_ => CSharpSyntaxTree.ParseText(
                _.Content,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
                _.Path)),
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    protected static INamedTypeSymbol TypeNamed(Compilation compilation, string metadataName) =>
        compilation.GetTypeByMetadataName(metadataName)!;

    protected sealed record SourceFile(string Path, string Content);
}
