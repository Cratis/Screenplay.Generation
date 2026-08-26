// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_legacy_authored_tree_paths_are_not_unique : given.a_compilation
{
    Exception[] _exceptions = null!;

    async Task Because()
    {
        var duplicateCompilation = CompilationFrom(
            new SourceFile("/workspace/Shared.cs", "namespace Banking; public static class First { public static void Run() => Query(); static void Query() { } }"),
            new SourceFile("/workspace/Shared.cs", "namespace Banking; public static class Second { static int Value; public static void Run() => Value = 42; }"));
        var emptyCompilation = CompilationFrom(
            new SourceFile(string.Empty, "namespace Banking; public static class Empty { public static void Run() => Query(); static void Query() { } }"));
        var duplicateTrees = duplicateCompilation.SyntaxTrees.ToArray();
        var first = duplicateTrees[0];
        var second = duplicateTrees[1];
        var empty = emptyCompilation.SyntaxTrees.Single();
        var forward = Project(duplicateCompilation, new HashSet<SyntaxTree> { first, second });
        var reversed = Project(duplicateCompilation, new HashSet<SyntaxTree> { second, first });
        var withoutPath = Project(emptyCompilation, new HashSet<SyntaxTree> { empty });

        _exceptions =
        [
            await Catch.Exception(() => Task.FromResult(DotNetSource.AuthoredInvocationsIn(forward))),
            await Catch.Exception(() => Task.FromResult(DotNetSource.AuthoredAssignmentsIn(reversed))),
            await Catch.Exception(() => Task.FromResult(DotNetSource.AuthoredInvocationsIn(withoutPath)))
        ];
    }

    [Fact] void should_fail_every_arrangement_with_the_typed_identity_exception() => _exceptions.All(_ => _ is DotNetAuthoredSyntaxTreeIdentityNotUnique).ShouldBeTrue();
    [Fact] void should_report_the_same_stable_message() => _exceptions.Select(_ => _.Message).Distinct(StringComparer.Ordinal).Count().ShouldEqual(1);

    static DotNetProjectCompilation Project(Compilation compilation, IReadOnlySet<SyntaxTree> authored) => new()
    {
        Name = compilation.AssemblyName ?? "Project",
        Compilation = compilation,
        AuthoredSyntaxTrees = authored
    };
}
