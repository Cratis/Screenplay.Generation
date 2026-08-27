// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetMethodSignatures;

public class when_signature_dimensions_differ : given.a_compilation
{
    IMethodSymbol _method = null!;
    IMethodSymbol _lookalike = null!;
    DotNetMethodSignature _expected = null!;
    ITypeSymbol _integer = null!;
    ITypeSymbol _string = null!;
    ITypeSymbol _nullableString = null!;
    ITypeSymbol _nullableStringParameter = null!;
    ITypeSymbol _unrelatedGenericArray = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Framework/Methods.cs",
            """
            namespace Framework;

            public static class Expected
            {
                public static string Match<T>(this int source, ref int count, params T[] values) => source.ToString();
                public static string Echo(string value) => value;
                public static string? Nullable() => null;
                public static void NullableParameter(string? value) => _ = value;
            }

            public static class Lookalike
            {
                public static string Match<T>(this int source, ref int count, params T[] values) => source.ToString();
            }
            """));
        _method = compilation.GetTypeByMetadataName("Framework.Expected")!.GetMembers("Match").OfType<IMethodSymbol>().Single();
        _lookalike = compilation.GetTypeByMetadataName("Framework.Lookalike")!.GetMembers("Match").OfType<IMethodSymbol>().Single();
        _expected = DotNetMethodSignatures.From(_method);
        _integer = compilation.GetSpecialType(SpecialType.System_Int32);
        _string = compilation.GetTypeByMetadataName("Framework.Expected")!.GetMembers("Echo").OfType<IMethodSymbol>().Single().Parameters[0].Type;
        _nullableString = compilation.GetTypeByMetadataName("Framework.Expected")!.GetMembers("Nullable").OfType<IMethodSymbol>().Single().ReturnType;
        _nullableStringParameter = compilation.GetTypeByMetadataName("Framework.Expected")!.GetMembers("NullableParameter").OfType<IMethodSymbol>().Single().Parameters[0].Type;
        _unrelatedGenericArray = _lookalike.Parameters[2].Type;
    }

    [Fact] void should_reject_an_unrelated_containing_type() => DotNetMethodSignatures.Matches(_lookalike, _expected).ShouldBeFalse();
    [Fact] void should_reject_a_different_method_name() => DotNetMethodSignatures.Matches(_method, _expected with { Name = "Other" }).ShouldBeFalse();
    [Fact] void should_reject_a_different_method_kind() => DotNetMethodSignatures.Matches(_method, _expected with { MethodKind = (MethodKind)((int)MethodKind.Ordinary + 1) }).ShouldBeFalse();
    [Fact] void should_reject_a_different_static_shape() => DotNetMethodSignatures.Matches(_method, _expected with { IsStatic = false }).ShouldBeFalse();
    [Fact] void should_reject_a_different_extension_shape() => DotNetMethodSignatures.Matches(_method, _expected with { IsExtensionMethod = false }).ShouldBeFalse();
    [Fact] void should_reject_a_different_generic_arity() => DotNetMethodSignatures.Matches(_method, _expected with { GenericArity = 2 }).ShouldBeFalse();
    [Fact] void should_reject_a_different_return_type() => DotNetMethodSignatures.Matches(_method, _expected with { ReturnType = _integer }).ShouldBeFalse();
    [Fact] void should_reject_different_return_nullability() => DotNetMethodSignatures.Matches(_method, _expected with { ReturnType = _nullableString }).ShouldBeFalse();
    [Fact] void should_reject_a_different_return_reference_kind() => DotNetMethodSignatures.Matches(_method, _expected with { ReturnRefKind = RefKind.Ref }).ShouldBeFalse();
    [Fact] void should_reject_a_different_parameter_count() => DotNetMethodSignatures.Matches(_method, _expected with { Parameters = [.. _expected.Parameters.Take(2)] }).ShouldBeFalse();
    [Fact] void should_reject_a_different_parameter_order() => DotNetMethodSignatures.Matches(_method, _expected with { Parameters = [.. _expected.Parameters.Reverse()] }).ShouldBeFalse();
    [Fact] void should_reject_a_different_parameter_type() => DotNetMethodSignatures.Matches(_method, _expected with { Parameters = ChangedParameter(0, _expected.Parameters[0] with { Type = _string }) }).ShouldBeFalse();
    [Fact] void should_reject_a_distinct_generic_type_parameter_symbol() => DotNetMethodSignatures.Matches(_method, _expected with { Parameters = ChangedParameter(2, _expected.Parameters[2] with { Type = _unrelatedGenericArray }) }).ShouldBeFalse();
    [Fact] void should_reject_different_parameter_nullability() => DotNetMethodSignatures.Matches(EchoMethod(), DotNetMethodSignatures.From(EchoMethod()) with { Parameters = [new DotNetParameterSignature { Type = _nullableStringParameter, RefKind = RefKind.None, IsParams = false, IsExtensionReceiver = false }] }).ShouldBeFalse();
    [Fact] void should_reject_a_different_parameter_reference_kind() => DotNetMethodSignatures.Matches(_method, _expected with { Parameters = ChangedParameter(1, _expected.Parameters[1] with { RefKind = RefKind.Out }) }).ShouldBeFalse();
    [Fact] void should_reject_a_different_params_shape() => DotNetMethodSignatures.Matches(_method, _expected with { Parameters = ChangedParameter(2, _expected.Parameters[2] with { IsParams = false }) }).ShouldBeFalse();
    [Fact] void should_reject_a_different_receiver_shape() => DotNetMethodSignatures.Matches(_method, _expected with { Parameters = ChangedParameter(0, _expected.Parameters[0] with { IsExtensionReceiver = false }) }).ShouldBeFalse();

    IReadOnlyList<DotNetParameterSignature> ChangedParameter(int index, DotNetParameterSignature parameter) =>
        [.. _expected.Parameters.Select((item, itemIndex) => itemIndex == index ? parameter : item)];

    IMethodSymbol EchoMethod() =>
        _method.ContainingType.GetMembers("Echo").OfType<IMethodSymbol>().Single();
}
