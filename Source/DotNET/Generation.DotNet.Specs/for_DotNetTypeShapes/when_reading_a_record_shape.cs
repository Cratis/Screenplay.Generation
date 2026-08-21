// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeShapes;

public class when_reading_a_record_shape : given.a_compilation
{
    IReadOnlyList<PropertyDefinition> _properties = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Banking/AccountOpened.cs",
            """
            namespace Banking;
            public record AccountOpened(
                System.Guid AccountId,
                string? Description,
                System.Collections.Generic.IReadOnlyList<decimal> Amounts,
                System.DateOnly OpenedOn);
            """));

        _properties = DotNetTypeShapes.PropertiesOf(TypeNamed(compilation, "Banking.AccountOpened"));
    }

    [Fact] void should_keep_declaration_order() => _properties.Select(_ => _.Name).ShouldEqual(["accountId", "description", "amounts", "openedOn"]);
    [Fact] void should_map_guid_to_uuid() => _properties[0].Type.Name.ShouldEqual("Uuid");
    [Fact] void should_keep_the_optional_string() => _properties[1].Type.IsOptional.ShouldBeTrue();
    [Fact] void should_map_the_collection_element() => _properties[2].Type.Name.ShouldEqual("Decimal");
    [Fact] void should_mark_the_collection() => _properties[2].Type.IsCollection.ShouldBeTrue();
    [Fact] void should_map_date_only_to_date() => _properties[3].Type.Name.ShouldEqual("Date");
}
