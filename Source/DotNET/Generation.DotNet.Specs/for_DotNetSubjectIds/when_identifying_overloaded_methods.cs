// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSubjectIds;

public class when_identifying_overloaded_methods : given.a_compilation
{
    SubjectId _first = null!;
    SubjectId _second = null!;
    SubjectId _complex = null!;
    string _displayName = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Messaging/NotificationHandler.cs",
            """
            namespace Messaging;
            public record First;
            public record Second;
            public sealed class NotificationHandler
            {
                public static void Handle(First message) { }
                public static void Handle(Second message) { }
                public static void Transform<T>(ref T value, in int version, params string[] tags) { }
            }
            """));
        var type = TypeNamed(compilation, "Messaging.NotificationHandler");
        var methods = type.GetMembers().OfType<IMethodSymbol>().ToArray();
        var overloads = methods.Where(_ => _.Name == "Handle").ToArray();
        var complex = methods.Single(_ => _.Name == "Transform");

        _first = DotNetSubjectIds.ForMethod(overloads[0], "Messaging/Application");
        _second = DotNetSubjectIds.ForMethod(overloads[1], "Messaging/Application");
        _complex = DotNetSubjectIds.ForMethod(complex, "Messaging/Application");
        _displayName = DotNetSubjectIds.MethodDisplayName(complex);
    }

    [Fact] void should_distinguish_overloads() => _first.ShouldNotEqual(_second);
    [Fact] void should_include_the_project_identity() => _first.Value.StartsWith("dotnet://Messaging%2FApplication/", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_a_documentation_method_identity() => _complex.Value.ShouldContain("#method:M%3A");
    [Fact] void should_keep_generic_arity_and_parameter_modifiers_readable() => _displayName.ShouldEqual("NotificationHandler.Transform`1(ref T, in int, params string[])");
}
