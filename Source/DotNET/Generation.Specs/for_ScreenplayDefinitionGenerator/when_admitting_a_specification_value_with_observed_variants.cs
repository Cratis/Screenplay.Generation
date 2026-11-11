// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_admitting_a_specification_value_with_observed_variants : Specification
{
    readonly List<string> _reads = [];
    bool _admitted;
    AdmittedSpecificationValue? _value;

    void Because()
    {
        var step = new SpecificationStepKey
        {
            Scenario = new SpecificationScenarioKey { Scenario = new SubjectId { Value = "dotnet://Banking/Specs.O01" } },
            Index = 0
        };
        var root = Definition(step, ["address"], SpecificationValueKind.Composite, null, ["address", "street"], ["address", "city"]);
        var street = Definition(step, ["address", "street"], SpecificationValueKind.Text, "Main");
        var city = Definition(step, ["address", "city"], SpecificationValueKind.Text, "Oslo");
        var type = typeof(GenerationResolver).Assembly.GetType("Cratis.Screenplay.Generation.SpecificationValueAdmission")!;
        List<ResolvedSpecificationValue> values = [Observe(root), Observe(street), Observe(city)];
        var admission = Activator.CreateInstance(type, values)!;
        object?[] arguments = [root.Key, step, null];
        _admitted = (bool)type.GetMethod("TryAdmit", BindingFlags.Public | BindingFlags.Instance)!.Invoke(admission, arguments)!;
        _value = (AdmittedSpecificationValue?)arguments[2];
    }

    [Fact] void should_admit_the_value() => _admitted.ShouldBeTrue();

    [Fact]
    void should_admit_the_children_in_declaration_order() => string.Join(
        '\n',
        _value!.Children.Select(_ => $"{string.Join('.', _.Definition.Key.Path)} = {_.Definition.Scalar}")).ShouldEqual(
        """
        address.street = Main
        address.city = Oslo
        """.ReplaceLineEndings("\n"));

    [Fact]
    void should_read_the_variants_of_every_value_once() => string.Join('\n', _reads).ShouldEqual(
        """
        address:count
        address:enumerate
        address.street:count
        address.street:enumerate
        address.city:count
        address.city:enumerate
        """);

    static SpecificationValueDefinition Definition(
        SpecificationStepKey step,
        string[] path,
        SpecificationValueKind kind,
        string? scalar,
        params string[][] children) => new()
        {
            Key = new SpecificationValueKey { Step = step, Path = path },
            Kind = kind,
            Scalar = scalar,
            Children = [.. children.Select(_ => new SpecificationValueKey { Step = step, Path = _ })]
        };

    ResolvedSpecificationValue Observe(SpecificationValueDefinition definition) => new()
    {
        Key = definition.Key,
        Variants = new given.an_observed_list<ResolvedSpecificationValueVariant>(
            [new ResolvedSpecificationValueVariant { Definition = definition }],
            read => _reads.Add($"{string.Join('.', definition.Key.Path)}:{read}"))
    };
}
