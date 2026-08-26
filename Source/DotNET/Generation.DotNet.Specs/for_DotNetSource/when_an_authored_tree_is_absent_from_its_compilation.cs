// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_an_authored_tree_is_absent_from_its_compilation : given.a_compilation
{
    Exception _exception = null!;

    async Task Because()
    {
        var compilation = CompilationFrom(new SourceFile("/workspace/Banking/Account.cs", "namespace Banking; public record Account;"));
        var missing = CSharpSyntaxTree.ParseText("namespace Banking; public static class Queries { public static void Run() => Execute(); static void Execute() { } }", path: "/workspace/Banking/Missing.cs");
        var project = new DotNetProjectCompilation
        {
            Name = "Banking",
            Compilation = compilation,
            AuthoredSyntaxTrees = new HashSet<SyntaxTree> { compilation.SyntaxTrees.Single(), missing }
        };

        _exception = await Catch.Exception(() => Task.FromResult(DotNetSource.AuthoredInvocationsIn(project)));
    }

    [Fact] void should_fail_with_the_typed_source_contract_exception() => _exception.ShouldBeOfExactType<DotNetAuthoredSyntaxTreeNotInCompilation>();
    [Fact] void should_name_the_missing_tree() => _exception.Message.ShouldContain("Missing.cs");
}
