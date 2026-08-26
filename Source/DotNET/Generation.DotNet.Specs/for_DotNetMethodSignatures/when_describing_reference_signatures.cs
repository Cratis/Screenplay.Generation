// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetMethodSignatures;

public class when_describing_reference_signatures : given.a_compilation
{
    DotNetMethodSignature _signature = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Framework/ReferenceMethods.cs",
            """
            namespace Framework;

            public static class ReferenceMethods
            {
                public static ref readonly int Read(in int value) => ref value;
            }
            """));
        var method = compilation.GetTypeByMetadataName("Framework.ReferenceMethods")!.GetMembers("Read").OfType<IMethodSymbol>().Single();
        _signature = DotNetMethodSignatures.From(method);
    }

    [Fact] void should_preserve_the_return_reference_kind() => _signature.ReturnRefKind.ShouldEqual(RefKind.In);
    [Fact] void should_preserve_the_parameter_reference_kind() => _signature.Parameters.Single().RefKind.ShouldEqual(RefKind.In);
}
