// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSymbols;

public class when_reading_attribute_named_arguments : given.a_compilation
{
    bool _foundRequired;
    bool _required;
    string _name = null!;
    string _missing = null!;
    bool? _optionalRequired;
    bool? _optionalMissing;
    bool _foundNullReference;
    string? _nullReference = "sentinel";
    bool _foundWithWrongType;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Messaging/Endpoint.cs",
            """
            namespace Messaging;
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public sealed class EndpointPolicyAttribute : System.Attribute
            {
                public bool Required { get; set; }
                public string Name { get; set; } = string.Empty;
                public string? Optional { get; set; }
            }

            [EndpointPolicy(Required = true, Name = "Licensed", Optional = null)]
            public sealed class Endpoint;
            """));
        var attribute = TypeNamed(compilation, "Messaging.Endpoint").GetAttributes().Single();

        _foundRequired = DotNetSymbols.TryNamedArgument(attribute, "Required", out _required);
        _name = DotNetSymbols.NamedArgument(attribute, "Name", "fallback");
        _missing = DotNetSymbols.NamedArgument(attribute, "Missing", "fallback");
        _optionalRequired = DotNetSymbols.NamedArgument<bool>(attribute, "Required");
        _optionalMissing = DotNetSymbols.NamedArgument<bool>(attribute, "Missing");
        _foundNullReference = DotNetSymbols.TryNamedArgument(attribute, "Optional", out _nullReference);
        _foundWithWrongType = DotNetSymbols.TryNamedArgument(attribute, "Name", out bool _);
    }

    [Fact] void should_find_a_typed_value() => _foundRequired.ShouldBeTrue();
    [Fact] void should_read_a_boolean() => _required.ShouldBeTrue();
    [Fact] void should_read_a_reference_value() => _name.ShouldEqual("Licensed");
    [Fact] void should_return_the_explicit_fallback_for_a_missing_argument() => _missing.ShouldEqual("fallback");
    [Fact] void should_return_an_optional_value() => _optionalRequired.ShouldEqual(true);
    [Fact] void should_return_null_for_a_missing_optional_value() => _optionalMissing.ShouldBeNull();
    [Fact] void should_distinguish_an_explicit_null_reference() => _foundNullReference.ShouldBeTrue();
    [Fact] void should_return_the_explicit_null_reference() => _nullReference.ShouldBeNull();
    [Fact] void should_reject_a_value_of_another_type() => _foundWithWrongType.ShouldBeFalse();
}
