// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_enumerating_authored_invocations_and_assignments : given.a_compilation
{
    IReadOnlyList<InvocationExpressionSyntax> _invocations = null!;
    IReadOnlyList<AssignmentExpressionSyntax> _assignments = null!;
    IReadOnlyList<InvocationExpressionSyntax> _scopedInvocations = null!;
    IReadOnlyList<AssignmentExpressionSyntax> _scopedAssignments = null!;
    IReadOnlyList<InvocationExpressionSyntax> _generatedScopeInvocations = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            new SourceFile("/checkout/Banking/B.cs", """
                namespace Banking;
                public static partial class Queries
                {
                    public static void Second() => Result = Query("second");
                }
                """),
            new SourceFile("/checkout/Banking/A.cs", """
                namespace Banking;
                public static partial class Queries
                {
                    public static string Result { get; set; } = string.Empty;
                    public static string Query(string value) => value;
                    public static void First() => Result = Query("first");
                }
                """),
            new SourceFile("/checkout/Banking/Queries.g.cs", """
                namespace Banking;
                public static partial class Queries
                {
                    public static void Generated() => Result = Query("generated");
                }
                """));
        var authored = compilation.SyntaxTrees.Where(tree => !tree.FilePath.EndsWith(".g.cs", StringComparison.Ordinal)).ToHashSet();
        var sourceContext = ContextFor(authored);
        var project = new DotNetProjectCompilation
        {
            Name = "Banking",
            Compilation = compilation,
            AuthoredSyntaxTrees = authored,
            SourceContext = sourceContext
        };
        var firstScope = authored.Single(tree => tree.FilePath.EndsWith("/A.cs", StringComparison.Ordinal)).GetRoot();
        var generatedScope = compilation.SyntaxTrees.Single(tree => tree.FilePath.EndsWith(".g.cs", StringComparison.Ordinal)).GetRoot();

        _invocations = DotNetSource.AuthoredInvocationsIn(project);
        _assignments = DotNetSource.AuthoredAssignmentsIn(project);
        _scopedInvocations = DotNetSource.AuthoredInvocationsIn(firstScope, project);
        _scopedAssignments = DotNetSource.AuthoredAssignmentsIn(firstScope, project);
        _generatedScopeInvocations = DotNetSource.AuthoredInvocationsIn(generatedScope, project);
    }

    [Fact] void should_enumerate_only_authored_invocations_in_stable_source_identity_order() => _invocations.Select(Argument).ShouldEqual(["first", "second"]);
    [Fact] void should_enumerate_only_authored_assignments_in_stable_source_identity_order() => _assignments.Select(_ => _.Right.ToString()).ShouldEqual(["Query(\"first\")", "Query(\"second\")"]);
    [Fact] void should_limit_scoped_invocations_to_the_exact_authored_scope() => _scopedInvocations.Select(Argument).ShouldContainOnly("first");
    [Fact] void should_limit_scoped_assignments_to_the_exact_authored_scope() => _scopedAssignments.Count.ShouldEqual(1);
    [Fact] void should_return_no_candidates_for_a_generated_scope() => _generatedScopeInvocations.ShouldBeEmpty();

    static string Argument(InvocationExpressionSyntax invocation) =>
        ((LiteralExpressionSyntax)invocation.ArgumentList.Arguments.Single().Expression).Token.ValueText;

    static DotNetProjectSourceContext ContextFor(IReadOnlySet<SyntaxTree> trees) => DotNetSourcePaths.Create(
        "Banking/Banking",
        new DotNetSourcePathPolicy
        {
            DisplayRoot = DotNetSourceDisplayRoot.Workspace,
            CasePolicy = DotNetSourcePathCasePolicy.Ordinal
        },
        [
            .. trees.Select(tree => new DotNetSourceDocument
            {
                SyntaxTree = tree,
                ProjectRelativePath = Path.GetFileName(tree.FilePath),
                WorkspaceRelativePath = $"apps/Banking/{Path.GetFileName(tree.FilePath)}"
            })
        ]);
}
