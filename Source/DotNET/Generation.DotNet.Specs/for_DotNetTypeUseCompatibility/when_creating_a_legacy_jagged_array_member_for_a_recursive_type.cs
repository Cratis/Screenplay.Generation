// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeUseCompatibility;

public class when_creating_a_legacy_jagged_array_member_for_a_recursive_type : given.a_compilation
{
    bool _completed;
    bool _marked;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Ordering/Recursive.cs",
            """
            namespace Ordering;
            public sealed class Recursive : System.Collections.Generic.List<Recursive>;
            public sealed record Holder(Recursive Value);
            """));
        var project = new DotNetProjectCompilation
        {
            Name = "Ordering",
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
        };
        var context = new DotNetAnalysisContext([project]);
        var artifact = new ArtifactKey { Subject = new SubjectId { Value = "dotnet://Ordering/Events.Holder" }, Kind = ArtifactKind.Event };
        var type = TypeNamed(compilation, "Ordering.Recursive");
        var member = new ArtifactMemberKey { Artifact = artifact, Name = "Value" };
        var work = Task.Run(() => DotNetTypeUseCompatibility.TryCreateLegacyJaggedArrayMember(type, context, member, out _));
        _completed = work.Wait(TimeSpan.FromSeconds(5));
        _marked = _completed && work.Result;
    }

    [Fact] void should_return_without_walking_the_recursive_type() => _completed.ShouldBeTrue();
    [Fact] void should_not_mark_the_type() => _marked.ShouldBeFalse();
}
