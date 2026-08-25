// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSymbols;

public class when_matching_companion_method_families : given.a_compilation
{
    IReadOnlyList<IMethodSymbol> _methods = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Messaging/RegisterHandler.cs",
            """
            namespace Messaging;
            public record Register;
            public record Rename;
            public sealed class RegisterHandler
            {
                public static void Validate(Register request) { }
                private static void Load(Register request, int version) { }
                public static void Validate(Rename request) { }
                public static void Validate() { }
                public static void Handle(Register request) { }
            }
            """));
        var handler = TypeNamed(compilation, "Messaging.RegisterHandler");
        var request = TypeNamed(compilation, "Messaging.Register");

        _methods = DotNetSymbols.CompanionMethodsFor(handler, request, ["Validate", "Load"]);
    }

    [Fact] void should_match_only_allowlisted_names() => _methods.Select(_ => _.Name).ShouldEqual(["Load", "Validate"]);
    [Fact] void should_match_the_exact_first_parameter_type() => _methods.All(_ => _.Parameters[0].Type.Name == "Register").ShouldBeTrue();
    [Fact] void should_keep_methods_declared_on_the_companion_type() => _methods.All(_ => _.ContainingType.Name == "RegisterHandler").ShouldBeTrue();
}
