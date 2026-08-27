// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_mutating_admitted_input : given.a_contribution
{
    readonly List<PropertyDefinition> _properties =
    [
        new PropertyDefinition
        {
            Name = "second",
            Type = new TypeReferenceDefinition { Name = "External", Subject = ExternalSubject }
        },
        new PropertyDefinition
        {
            Name = "first",
            Type = new TypeReferenceDefinition { Name = "String" }
        }
    ];
    readonly List<string> _path = ["arguments", "name"];
    List<GenerationFact> _facts = null!;
    GenerationFact _originalFact = null!;
    AdapterContributionAdmissionResult _result = null!;

    void Establish()
    {
        _facts = EveryFact(_properties, valuePath: _path);
        _originalFact = _facts[0];
    }

    void Because()
    {
        _result = Admit(contribution: Contribution(_facts));
        _facts.Clear();
        _properties.Clear();
        _path.Reverse();
        _path.Clear();
    }

    [Fact] void should_keep_the_frozen_fact_list() => _result.Snapshot!.Facts.Length.ShouldEqual(9);
    [Fact] void should_deep_copy_fact_records() => ReferenceEquals(_originalFact, _result.Snapshot!.Facts.Single(fact => fact.Id.Value == "atomic:artifact")).ShouldBeFalse();
    [Fact] void should_keep_nested_properties_in_authored_order() => _result.Snapshot!.Facts.OfType<ArtifactFact>().Single().Definition.Properties.Select(property => property.Name).ShouldEqual(["second", "first"]);
    [Fact] void should_keep_nested_value_paths_in_authored_order() => string.Join('|', _result.Snapshot!.Facts.OfType<SpecificationValueFact>().Single().Definition.Key.Path).ShouldEqual("arguments|name");
}
