// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetMethodSignatures;

public class when_parameter_signatures_are_snapshotted : given.a_compilation
{
    List<DotNetParameterSignature> _mutable = null!;
    DotNetMethodSignature _signature = null!;

    void Establish()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Signatures.cs",
            """
            namespace Values;
            public static class Operations
            {
                public static string Convert(int value) => value.ToString();
            }
            """));
        var method = compilation.GetTypeByMetadataName("Values.Operations")!.GetMembers("Convert").OfType<IMethodSymbol>().Single();
        var described = DotNetMethodSignatures.From(method);
        _mutable = [.. described.Parameters];
        _signature = described with { Parameters = _mutable };
    }

    void Because() => _mutable.Clear();

    [Fact] void should_retain_the_parameter_snapshot() => _signature.Parameters.Count.ShouldEqual(1);
    [Fact] void should_expose_an_immutable_parameter_surface() => _signature.Parameters.GetType().ShouldEqual(typeof(ImmutableArray<DotNetParameterSignature>));
}
