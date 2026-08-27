// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_bound_expressions_have_compiler_errors : given.a_compilation
{
    DotNetBounded<DotNetSourceValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/CompilerErrors.cs",
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            namespace Values;

            [Obsolete("No longer supported", true)]
            public sealed class LegacyType;

            public sealed class LegacyPayload
            {
                [Obsolete("No longer supported", true)]
                public LegacyPayload(int value) => _ = value;
            }

            public sealed class LegacyCollection : IEnumerable<int>
            {
                [Obsolete("No longer supported", true)]
                public void Add(int value) { }

                public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)Array.Empty<int>()).GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            public static class Usage
            {
                [Obsolete("No longer supported", true)]
                public const int LegacyValue = 42;

                public static int Scalar => LegacyValue;
                public static Type Type => typeof(LegacyType);
                public static LegacyPayload Payload => new(42);
                public static LegacyCollection Collection => new() { 42 };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _results =
        [
            .. tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Where(clause => clause.Parent is PropertyDeclarationSyntax)
                .Select(clause => DotNetSourceValues.Extract(clause.Expression, semanticModel))
        ];
    }

    [Fact] void should_publish_no_known_value_from_any_bound_compiler_error() => _results.All(_ => _ is DotNetUnknown<DotNetSourceValue>).ShouldBeTrue();
    [Fact] void should_report_one_unsupported_failure_for_each_bound_error() => _results.SelectMany(Failures).Select(_ => _.Kind).ShouldEqual(Enumerable.Repeat(DotNetValueFailureKind.Unsupported, 4));
    [Fact] void should_locate_each_failure_at_its_complete_authored_expression() => string.Join('|', _results.SelectMany(Failures).Select(_ => SourceText(_.Source))).ShouldEqual("LegacyValue|typeof(LegacyType)|new(42)|42");

    static IReadOnlyList<DotNetValueFailure> Failures(DotNetBounded<DotNetSourceValue> result) => ((DotNetUnknown<DotNetSourceValue>)result).Failures;
    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
