// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_extracting_exact_scalar_values : given.a_compilation
{
    DotNetConstantValue[] _values = null!;
    DotNetBounded<string> _typed = null!;
    DotNetBounded<string?> _typedNull = null!;
    DotNetBounded<byte> _typedByte = null!;
    DotNetBounded<long> _typedLong = null!;
    DotNetBounded<int> _typedEnum = null!;
    DotNetBounded<object> _typedObject = null!;
    DotNetConstantValue _contextualEnum = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Values.cs",
            """
            namespace Values;

            public enum ProjectStatus
            {
                Active = 2,
                Enabled = 2
            }

            public static class Constants
            {
                public const string Name = "Screenplay";
                public const int Number = 42;
            }

            public static class Usage
            {
                static void Inspect<T>(T value) { }
                static void InspectByte(byte value) { }
                static void InspectLong(long value) { }

                public static void Run()
                {
                    Inspect("literal");
                    Inspect(Constants.Name);
                    Inspect(Constants.Number);
                    Inspect(ProjectStatus.Active);
                    Inspect<string?>(null);
                    Inspect(nameof(Usage));
                    Inspect<object>(ProjectStatus.Enabled);
                    InspectByte(1);
                    InspectLong(2);
                }
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var invocations = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        var arguments = invocations
            .Where(invocation => invocation.Expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText == "Inspect",
                GenericNameSyntax generic => generic.Identifier.ValueText == "Inspect" && generic.TypeArgumentList.Arguments.Single().ToString() != "object",
                _ => false
            })
            .Select(invocation => invocation.ArgumentList.Arguments.Single().Expression)
            .ToArray();
        _values =
        [
            .. arguments.Select(expression => ((DotNetKnown<DotNetSourceValue>)DotNetSourceValues.Extract(expression, semanticModel)).Value)
                .Cast<DotNetConstantValue>()
        ];
        _typed = DotNetSourceValues.Constant<string>(arguments[1], semanticModel);
        _typedNull = DotNetSourceValues.Constant<string?>(arguments[4], semanticModel);
        _typedByte = DotNetSourceValues.Constant<byte>(invocations.Single(invocation => invocation.Expression.ToString() == "InspectByte").ArgumentList.Arguments.Single().Expression, semanticModel);
        _typedLong = DotNetSourceValues.Constant<long>(invocations.Single(invocation => invocation.Expression.ToString() == "InspectLong").ArgumentList.Arguments.Single().Expression, semanticModel);
        _typedEnum = DotNetSourceValues.Constant<int>(arguments[3], semanticModel);
        _typedObject = DotNetSourceValues.Constant<object>(arguments[0], semanticModel);
        var contextualEnum = invocations.Single(invocation => invocation.Expression.ToString() == "Inspect<object>").ArgumentList.Arguments.Single().Expression;
        _contextualEnum = (DotNetConstantValue)((DotNetKnown<DotNetSourceValue>)DotNetSourceValues.Extract(contextualEnum, semanticModel)).Value;
    }

    [Fact] void should_extract_literal_and_named_string_constants() => _values.Take(2).Select(_ => _.Value).ShouldEqual(["literal", "Screenplay"]);
    [Fact] void should_extract_numeric_constants() => _values[2].Value.ShouldEqual(42);
    [Fact] void should_preserve_the_exact_enum_member_symbol() => ((IFieldSymbol)_values[3].Value!).Name.ShouldEqual("Active");
    [Fact] void should_preserve_the_enum_type() => _values[3].Type!.Name.ShouldEqual("ProjectStatus");
    [Fact] void should_extract_null() => _values[4].Value.ShouldBeNull();
    [Fact] void should_extract_nameof_as_a_semantic_constant() => _values[5].Value.ShouldEqual("Usage");
    [Fact] void should_extract_a_typed_constant() => ((DotNetKnown<string>)_typed).Value.ShouldEqual("Screenplay");
    [Fact] void should_extract_a_typed_null() => ((DotNetKnown<string?>)_typedNull).Value.ShouldBeNull();
    [Fact] void should_apply_a_roslyn_proven_byte_conversion() => ((DotNetKnown<byte>)_typedByte).Value.ShouldEqual((byte)1);
    [Fact] void should_apply_a_roslyn_proven_long_conversion() => ((DotNetKnown<long>)_typedLong).Value.ShouldEqual(2L);
    [Fact] void should_not_expose_a_source_enum_as_its_underlying_runtime_number() => _typedEnum.ShouldBeOfExactType<DotNetUnknown<int>>();
    [Fact] void should_not_widen_a_typed_constant_to_object() => _typedObject.ShouldBeOfExactType<DotNetUnknown<object>>();
    [Fact] void should_preserve_a_direct_enum_member_under_contextual_object_conversion() => ((IFieldSymbol)_contextualEnum.Value!).Name.ShouldEqual("Enabled");
}
