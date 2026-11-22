// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_ConceptRepresentationDefinition;

public class when_serializing_optional_enumeration_numbers : Specification
{
    string _legacy = null!;
    string _numbered = null!;

    void Because()
    {
        var definition = new ConceptRepresentationDefinition
        {
            Concept = new SubjectId { Value = "dotnet://Orders/Status" },
            Kind = ConceptRepresentationKind.Enumeration,
            EnumerationValues = ["Pending", "Complete"]
        };
        _legacy = JsonSerializer.Serialize(definition);
        _numbered = JsonSerializer.Serialize(definition with { EnumerationNumbers = [-1, 4] });
    }

    [Fact] void should_preserve_the_legacy_json_bytes() => _legacy.ShouldEqual(/*lang=json*/ """{"Concept":{"Value":"dotnet://Orders/Status"},"Kind":1,"Primitive":null,"EnumerationValues":["Pending","Complete"]}""");
    [Fact] void should_preserve_explicit_numbers_in_declaration_order() => _numbered.ShouldEqual(/*lang=json*/ """{"Concept":{"Value":"dotnet://Orders/Status"},"Kind":1,"Primitive":null,"EnumerationValues":["Pending","Complete"],"EnumerationNumbers":[-1,4]}""");
}
