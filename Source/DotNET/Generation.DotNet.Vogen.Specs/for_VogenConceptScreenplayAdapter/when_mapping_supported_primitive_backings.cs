// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_mapping_supported_primitive_backings : given.a_vogen_compilation
{
    AdapterContribution _contribution = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            "Concepts",
            new SourceFile(
                "/workspace/Concepts/Primitives.cs",
                """
                namespace Concepts;
                [Vogen.ValueObject<System.Guid>] public partial struct UuidValue;
                [Vogen.ValueObject<string>] public partial struct TextValue;
                [Vogen.ValueObject<byte>] public partial struct ByteValue;
                [Vogen.ValueObject<sbyte>] public partial struct SignedByteValue;
                [Vogen.ValueObject<short>] public partial struct ShortValue;
                [Vogen.ValueObject<ushort>] public partial struct UnsignedShortValue;
                [Vogen.ValueObject<int>] public partial struct IntValue;
                [Vogen.ValueObject<uint>] public partial struct UnsignedIntValue;
                [Vogen.ValueObject<long>] public partial struct LongValue;
                [Vogen.ValueObject<ulong>] public partial struct UnsignedLongValue;
                [Vogen.ValueObject<decimal>] public partial struct DecimalValue;
                [Vogen.ValueObject<double>] public partial struct DoubleValue;
                [Vogen.ValueObject<float>] public partial struct FloatValue;
                [Vogen.ValueObject<bool>] public partial struct BooleanValue;
                [Vogen.ValueObject<System.DateOnly>] public partial struct DateValue;
                [Vogen.ValueObject<System.DateTime>] public partial struct DateTimeValue;
                [Vogen.ValueObject<System.DateTimeOffset>] public partial struct DateTimeOffsetValue;
                """));

        _contribution = Analyze(Project("Concepts.Project", compilation));
    }

    [Fact] void should_map_uuid() => PrimitiveFor("UuidValue").ShouldEqual(GenerationPrimitiveKind.Uuid);
    [Fact] void should_map_text() => PrimitiveFor("TextValue").ShouldEqual(GenerationPrimitiveKind.Text);
    [Fact] void should_map_all_integral_primitives() => NamesFor(GenerationPrimitiveKind.WholeNumber).ShouldContainOnly("ByteValue", "SignedByteValue", "ShortValue", "UnsignedShortValue", "IntValue", "UnsignedIntValue", "LongValue", "UnsignedLongValue");
    [Fact] void should_map_all_decimal_primitives() => NamesFor(GenerationPrimitiveKind.Number).ShouldContainOnly("DecimalValue", "DoubleValue", "FloatValue");
    [Fact] void should_map_boolean() => PrimitiveFor("BooleanValue").ShouldEqual(GenerationPrimitiveKind.Boolean);
    [Fact] void should_map_date() => PrimitiveFor("DateValue").ShouldEqual(GenerationPrimitiveKind.Date);
    [Fact] void should_map_date_and_time_primitives() => NamesFor(GenerationPrimitiveKind.DateTime).ShouldContainOnly("DateTimeValue", "DateTimeOffsetValue");
    [Fact] void should_not_emit_diagnostics() => _contribution.Diagnostics.ShouldBeEmpty();

    GenerationPrimitiveKind? PrimitiveFor(string name) =>
        RepresentationFor(_contribution, ConceptNamed(_contribution, name)).Definition.Primitive;

    IEnumerable<string> NamesFor(GenerationPrimitiveKind primitive) =>
        from representation in _contribution.Facts.OfType<ConceptRepresentationFact>()
        where representation.Definition.Primitive == primitive
        join artifact in _contribution.Facts.OfType<ArtifactFact>() on representation.Subject equals artifact.Subject
        select artifact.Definition.Name;
}
