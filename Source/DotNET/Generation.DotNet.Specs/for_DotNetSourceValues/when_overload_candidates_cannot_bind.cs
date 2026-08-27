// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_overload_candidates_cannot_bind : given.a_compilation
{
    CandidateReason _candidateReason;
    DotNetValueFailure _failure = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Overloads.cs",
            """
            namespace Values;

            public static class Usage
            {
                static string Pick(string value) => value;
                static string Pick(System.Uri value) => value.ToString();

                public static string Value => Pick(42);
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var expression = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
            .Single(clause => clause.Parent is PropertyDeclarationSyntax)
            .Expression;
        _candidateReason = semanticModel.GetSymbolInfo(expression).CandidateReason;
        _failure = ((DotNetUnknown<DotNetSourceValue>)DotNetSourceValues.Extract(expression, semanticModel)).Failures.Single();
    }

    [Fact] void should_reproduce_an_overload_resolution_failure() => _candidateReason.ShouldEqual(CandidateReason.OverloadResolutionFailure);
    [Fact] void should_classify_the_failed_candidate_set_as_unbound() => _failure.Kind.ShouldEqual(DotNetValueFailureKind.Unbound);
    [Fact] void should_locate_the_failure_at_the_incompatible_invocation() => _failure.Source.SourceTree!.GetText().ToString(_failure.Source.SourceSpan).ShouldEqual("Pick(42)");
}
