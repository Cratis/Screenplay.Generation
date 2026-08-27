// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_contextual_conversions_are_user_defined : given.a_compilation
{
    DotNetUnknown<DotNetSourceValue>[] _scalarAndTypeOf = null!;
    DotNetBounded<DotNetPayloadValue>[] _payloads = null!;
    DotNetBounded<DotNetCollectionValue>[] _collections = null!;
    DotNetBounded<DotNetSourceValue>[] _builtIn = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/UserDefinedConversions.cs",
            """
            using System;
            using System.Collections.Generic;

            namespace Values;

            public enum Mode
            {
                None = 0
            }

            public readonly struct Destination
            {
                public static implicit operator Destination(int value) => default;
                public static implicit operator Destination(Type value) => default;
                public static implicit operator Destination(RootPayload value) => default;
                public static implicit operator Destination(int[] value) => default;
            }

            public sealed class RootPayload(int value);
            public sealed class ConstructorPayload(Destination value);

            public sealed class InitializerPayload
            {
                public Destination Value { get; init; }
            }

            public sealed class DestinationCollection : List<Destination>;

            public static class Usage
            {
                static void InspectDestination(Destination value) { }
                static void InspectByte(byte value) { }
                static void InspectNullable(int? value) { }
                static void InspectObject(object value) { }
                static void InspectMode(Mode value) { }
                static void InspectString(string? value) { }
                static void InspectInt(int value) { }

                public static Destination RootPayload => new RootPayload(1);
                public static ConstructorPayload ConstructorChild => new ConstructorPayload(1);
                public static InitializerPayload InitializerChild => new() { Value = 1 };
                public static Destination RootArray => new int[] { 1 };
                public static Destination[] ArrayChild => new Destination[] { 1 };
                public static Destination[] ExpressionChild => [1];
                public static DestinationCollection InitializerChildCollection => new() { 1 };

                public static void Run()
                {
                    InspectDestination(1);
                    InspectDestination(typeof(string));
                    InspectByte(1);
                    InspectNullable(1);
                    InspectObject(1);
                    InspectMode(0);
                    InspectString(null);
                    InspectInt(1);
                }
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation => invocation.Expression.ToString().StartsWith("Inspect", StringComparison.Ordinal))
            .ToArray();
        _scalarAndTypeOf =
        [
            .. invocations.Take(2)
                .Select(invocation => (DotNetUnknown<DotNetSourceValue>)DotNetSourceValues.Extract(invocation.ArgumentList.Arguments.Single().Expression, semanticModel))
        ];
        _builtIn =
        [
            .. invocations.Skip(2)
                .Select(invocation => DotNetSourceValues.Extract(invocation.ArgumentList.Arguments.Single().Expression, semanticModel))
        ];

        var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Where(property => property.ExpressionBody is not null)
            .ToDictionary(property => property.Identifier.ValueText, property => property.ExpressionBody!.Expression, StringComparer.Ordinal);
        _payloads =
        [
            DotNetSourceValues.Payload(properties["RootPayload"], semanticModel),
            DotNetSourceValues.Payload(properties["ConstructorChild"], semanticModel),
            DotNetSourceValues.Payload(properties["InitializerChild"], semanticModel)
        ];
        _collections =
        [
            DotNetSourceValues.Collection(properties["RootArray"], semanticModel),
            DotNetSourceValues.Collection(properties["ArrayChild"], semanticModel),
            DotNetSourceValues.Collection(properties["ExpressionChild"], semanticModel),
            DotNetSourceValues.Collection(properties["InitializerChildCollection"], semanticModel)
        ];
    }

    [Fact] void should_reject_scalar_and_typeof_user_defined_conversions_once() => _scalarAndTypeOf.SelectMany(_ => _.Failures).Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_scalar_and_typeof_user_defined_conversions_at_their_authored_expressions() => _scalarAndTypeOf.SelectMany(_ => _.Failures).Select(_ => SourceText(_.Source)).ShouldEqual(["1", "typeof(string)"]);
    [Fact] void should_reject_payload_root_constructor_child_and_initializer_child_user_defined_conversions_once() => _payloads.SelectMany(PayloadFailures).Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_payload_user_defined_conversions_at_the_root_then_each_child() => _payloads.SelectMany(PayloadFailures).Select(_ => SourceText(_.Source)).ShouldEqual(["new RootPayload(1)", "1", "1"]);
    [Fact] void should_reject_collection_root_array_expression_and_initializer_child_user_defined_conversions_once() => _collections.SelectMany(CollectionFailures).Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_collection_user_defined_conversions_at_the_root_then_each_child() => _collections.SelectMany(CollectionFailures).Select(_ => SourceText(_.Source)).ShouldEqual(["new int[] { 1 }", "1", "1", "1"]);
    [Fact] void should_accept_builtin_numeric_nullable_boxing_enum_null_and_identity_conversions() => _builtIn.All(_ => _ is DotNetKnown<DotNetSourceValue>).ShouldBeTrue();
    [Fact] void should_publish_no_partial_payload_or_collection_values() => (_payloads.All(_ => _ is DotNetUnknown<DotNetPayloadValue>) && _collections.All(_ => _ is DotNetUnknown<DotNetCollectionValue>)).ShouldBeTrue();

    static IReadOnlyList<DotNetValueFailure> PayloadFailures(DotNetBounded<DotNetPayloadValue> result) => ((DotNetUnknown<DotNetPayloadValue>)result).Failures;

    static IReadOnlyList<DotNetValueFailure> CollectionFailures(DotNetBounded<DotNetCollectionValue> result) => ((DotNetUnknown<DotNetCollectionValue>)result).Failures;

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
