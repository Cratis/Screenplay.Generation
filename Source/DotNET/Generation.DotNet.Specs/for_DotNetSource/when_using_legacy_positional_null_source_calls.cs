// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_using_legacy_positional_null_source_calls : given.a_compilation
{
    Diagnostic[] _errors = null!;

    void Because()
    {
        var source = CSharpSyntaxTree.ParseText(
            """
            using System.Collections.Generic;
            using Cratis.Screenplay.Generation;
            using Cratis.Screenplay.Generation.DotNet;
            using Microsoft.CodeAnalysis;

            public static class LegacySourceCalls
            {
                public static void Compile(
                    Location location,
                    ISymbol symbol,
                    AttributeData attribute,
                    IReadOnlySet<SyntaxTree> authoredTrees,
                    AdapterIdentity adapter)
                {
                    _ = DotNetSource.Range(location, null);
                    _ = DotNetSource.EvidenceFor(symbol, adapter, EvidenceStrength.Exact, null);
                    _ = DotNetSource.EvidenceFor(symbol, adapter, EvidenceStrength.Exact, null, "Legacy symbol evidence");
                    _ = DotNetSource.EvidenceFor(attribute, adapter, EvidenceStrength.Exact, null);
                    _ = DotNetSource.EvidenceFor(attribute, adapter, EvidenceStrength.Exact, null, "Legacy attribute evidence");
                    _ = DotNetSource.EvidenceFor(attribute, authoredTrees, adapter, EvidenceStrength.Exact, null);
                    _ = DotNetSource.EvidenceFor(attribute, authoredTrees, adapter, EvidenceStrength.Exact, null, "Legacy authored attribute evidence");
                }
            }
            """);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator)
            .Append(typeof(SyntaxTree).Assembly.Location)
            .Append(typeof(CSharpSyntaxTree).Assembly.Location)
            .Append(typeof(Evidence).Assembly.Location)
            .Append(typeof(DotNetSource).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(_ => MetadataReference.CreateFromFile(_));
        var compilation = CSharpCompilation.Create(
            "LegacySourceCompatibility",
            [source],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        _errors = [.. compilation.GetDiagnostics().Where(_ => _.Severity == DiagnosticSeverity.Error)];
    }

    [Fact] void should_compile_without_overload_ambiguity() => _errors.ShouldBeEmpty();
}
