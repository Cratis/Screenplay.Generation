// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_enumerating_a_scope_without_unique_legacy_paths : given.a_compilation
{
    IReadOnlyList<AssignmentExpressionSyntax> _assignments = null!;
    IReadOnlyList<InvocationExpressionSyntax> _invocations = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            new SourceFile(string.Empty, """
                namespace Values;
                public static class First
                {
                    static string Transform(string value) => value;
                    public static string Result { get; set; } = string.Empty;
                    public static void Run()
                    {
                        Result = Transform("first");
                        Result = Transform("second");
                    }
                }
                """),
            new SourceFile(string.Empty, """
                namespace Values;
                public static class Second
                {
                    static string Transform(string value) => value;
                    public static string Result { get; set; } = string.Empty;
                    public static void Run() => Result = Transform("other");
                }
                """));
        var authoredTrees = compilation.SyntaxTrees.ToHashSet();
        var project = new DotNetProjectCompilation
        {
            Name = "Values",
            Compilation = compilation,
            AuthoredSyntaxTrees = authoredTrees
        };
        var scope = authoredTrees.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            .Single(declaration => string.Equals(declaration.Identifier.ValueText, "First", StringComparison.Ordinal))
            .Members.OfType<MethodDeclarationSyntax>()
            .Single(method => string.Equals(method.Identifier.ValueText, "Run", StringComparison.Ordinal));

        _invocations = DotNetSource.AuthoredInvocationsIn(scope, project);
        _assignments = DotNetSource.AuthoredAssignmentsIn(scope, project);
    }

    [Fact] void should_enumerate_scoped_invocations_without_cross_tree_identity() => _invocations.Select(Argument).ShouldEqual(["first", "second"]);
    [Fact] void should_enumerate_scoped_assignments_without_cross_tree_identity() => _assignments.Select(assignment => assignment.Right.ToString()).ShouldEqual(["Transform(\"first\")", "Transform(\"second\")"]);
    [Fact] void should_preserve_exact_syntax_order_within_the_scope() => _invocations.Select(invocation => invocation.SpanStart).ShouldEqual(_invocations.Select(invocation => invocation.SpanStart).Order());

    static string Argument(InvocationExpressionSyntax invocation) =>
        ((LiteralExpressionSyntax)invocation.ArgumentList.Arguments.Single().Expression).Token.ValueText;
}
