// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenMetadataNames;

public class when_binding_real_vogen_8_attributes : given.a_vogen_compilation
{
    AttributeData _defaults = null!;
    AttributeData _generic = null!;
    AttributeData _instance = null!;
    AttributeData _nonGeneric = null!;
    INamedTypeSymbol _validation = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            "Concepts",
            new SourceFile(
                "/workspace/Concepts/Metadata.cs",
                """
                [assembly: Vogen.VogenDefaults(underlyingType: typeof(string))]
                namespace Concepts;
                [Vogen.ValueObject<System.Guid>] public partial struct GenericValue;
                [Vogen.ValueObject(typeof(decimal))] public partial struct NonGenericValue;
                [Vogen.Instance("Unknown", "?")]
                [Vogen.ValueObject<string>] public partial struct ValueWithInstance;
                """));

        _defaults = DotNetSource.AuthoredAttributesOf(compilation.Assembly).Single();
        _generic = DotNetSource.AuthoredAttributesOf(compilation.GetTypeByMetadataName("Concepts.GenericValue")!).Single();
        _nonGeneric = DotNetSource.AuthoredAttributesOf(compilation.GetTypeByMetadataName("Concepts.NonGenericValue")!).Single();
        _instance = DotNetSource.AuthoredAttributesOf(compilation.GetTypeByMetadataName("Concepts.ValueWithInstance")!)
            .Single(_ => DotNetSubjectIds.MetadataName(_.AttributeClass!) == VogenMetadataNames.InstanceAttribute);
        _validation = compilation.GetTypeByMetadataName(VogenMetadataNames.Validation)!;
    }

    [Fact] void should_bind_the_exact_defaults_metadata_name() => DotNetSubjectIds.MetadataName(_defaults.AttributeClass!).ShouldEqual(VogenMetadataNames.DefaultsAttribute);
    [Fact] void should_bind_the_exact_generic_metadata_name() => DotNetSubjectIds.MetadataName(_generic.AttributeClass!).ShouldEqual(VogenMetadataNames.GenericValueObjectAttribute);
    [Fact] void should_take_the_generic_backing_from_type_argument_zero() => DotNetSubjectIds.MetadataName((INamedTypeSymbol)_generic.AttributeClass!.TypeArguments[0]).ShouldEqual("System.Guid");
    [Fact] void should_bind_the_exact_non_generic_metadata_name() => DotNetSubjectIds.MetadataName(_nonGeneric.AttributeClass!).ShouldEqual(VogenMetadataNames.ValueObjectAttribute);
    [Fact] void should_take_the_non_generic_backing_from_constructor_argument_zero() => ((ITypeSymbol)_nonGeneric.ConstructorArguments[0].Value!).SpecialType.ShouldEqual(SpecialType.System_Decimal);
    [Fact] void should_take_the_default_backing_from_constructor_argument_zero() => ((ITypeSymbol)_defaults.ConstructorArguments[0].Value!).SpecialType.ShouldEqual(SpecialType.System_String);
    [Fact] void should_bind_the_exact_instance_metadata_name() => DotNetSubjectIds.MetadataName(_instance.AttributeClass!).ShouldEqual(VogenMetadataNames.InstanceAttribute);
    [Fact] void should_bind_the_exact_validation_result_metadata_name() => DotNetSubjectIds.MetadataName(_validation).ShouldEqual(VogenMetadataNames.Validation);
}
