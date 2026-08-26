// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_enumerating_with_reversed_syntax_trees : given.a_compilation
{
    string[] _forwardInvocations = null!;
    string[] _reversedInvocations = null!;
    string[] _forwardAssignments = null!;
    string[] _reversedAssignments = null!;

    void Because()
    {
        (_forwardInvocations, _forwardAssignments) = Enumerate(reverse: false);
        (_reversedInvocations, _reversedAssignments) = Enumerate(reverse: true);
    }

    [Fact] void should_preserve_invocation_order() => _reversedInvocations.ShouldEqual(_forwardInvocations);
    [Fact] void should_preserve_assignment_order() => _reversedAssignments.ShouldEqual(_forwardAssignments);
    [Fact] void should_order_by_stable_source_identity() => _forwardInvocations.ShouldEqual(["A.cs:first", "B.cs:second"]);

    (string[] Invocations, string[] Assignments) Enumerate(bool reverse)
    {
        SourceFile[] sources =
        [
            new("/physical/checkout/B.cs", "namespace Banking; public static partial class Queries { public static void Second() => Result = Query(\"second\"); }"),
            new("/physical/checkout/A.cs", "namespace Banking; public static partial class Queries { public static string Result { get; set; } = string.Empty; public static string Query(string value) => value; public static void First() => Result = Query(\"first\"); }")
        ];
        if (reverse)
        {
            sources = [.. sources.AsEnumerable().Reverse()];
        }

        var compilation = CompilationFrom(sources);
        var trees = compilation.SyntaxTrees.ToHashSet();
        var sourceContext = DotNetSourcePaths.Create(
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
        var project = new DotNetProjectCompilation
        {
            Name = "Banking",
            Compilation = compilation,
            AuthoredSyntaxTrees = trees,
            SourceContext = sourceContext
        };
        string Identity(SyntaxNode node) => $"{sourceContext.Files[node.SyntaxTree].Identity.Path}:{((LiteralExpressionSyntax)((InvocationExpressionSyntax)node).ArgumentList.Arguments.Single().Expression).Token.ValueText}";
        var invocations = DotNetSource.AuthoredInvocationsIn(project).Select(Identity).ToArray();
        var assignments = DotNetSource.AuthoredAssignmentsIn(project).Select(assignment => $"{sourceContext.Files[assignment.SyntaxTree].Identity.Path}:{assignment.SpanStart}").ToArray();
        return (invocations, assignments);
    }
}
