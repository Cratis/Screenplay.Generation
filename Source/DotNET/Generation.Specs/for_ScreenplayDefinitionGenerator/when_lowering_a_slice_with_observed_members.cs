// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_a_slice_with_observed_members : given.a_specification_fixture
{
    readonly List<string> _reads = [];
    ScreenplayLoweringResult _result = null!;

    void Because()
    {
        var name = Property("name", "String");
        StepSpec when = new(SpecificationStepPhase.When, SpecificationStepKind.Command, _command, Values: [new("name", SpecificationValueKind.Text, "Alice")]);
        GenerationFact[] facts =
        [
            .. Artifact(ArtifactKind.Reducer, "AccountReducer", "Register", GenerationSliceKind.StateChange, 5, properties: [name]),
            .. Artifact(ArtifactKind.ReadModel, "AccountView", "Register", GenerationSliceKind.StateChange, 3, properties: [name]),
            .. Artifact(ArtifactKind.Query, "GetAccount", "Register", GenerationSliceKind.StateChange, 4, properties: [name]),
            .. Artifact(ArtifactKind.Event, "AccountRegistered", "Register", GenerationSliceKind.StateChange, 2, properties: [name]),
            .. Artifact(ArtifactKind.Command, "RegisterAccount", "Register", GenerationSliceKind.StateChange, 1, properties: [name]),
            .. Scenario("O01", _command, [when, new(SpecificationStepPhase.Then, SpecificationStepKind.Event, _event, Values: [new("name", SpecificationValueKind.Text, "Alice")])])
        ];
        var graph = new GenerationResolver().Resolve([Contribution(facts)]);
        var observed = graph with
        {
            Artifacts =
            [
                .. graph.Artifacts.Select(_ => _ with
                {
                    Variants =
                    [
                        .. _.Variants.Select(variant => variant with
                        {
                            Definition = variant.Definition with { Properties = Observe($"{_.Key.Kind}:{variant.Definition.Name}", variant.Definition.Properties) }
                        })
                    ]
                })
            ],
            Specifications = Observe("Specifications", graph.Specifications)
        };

        _result = new ScreenplayLowerer().Lower(observed, "Banking");
    }

    [Fact]
    void should_have_resolved_the_specification() => _result.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Count().ShouldEqual(1);

    [Fact]
    void should_lower_the_members_in_the_original_construction_order() => string.Join('\n', LastReadOrder()).ShouldEqual(
        """
        Command:RegisterAccount
        Event:AccountRegistered
        Query:GetAccount
        ReadModel:AccountView
        Reducer:AccountReducer
        Specifications
        """);

    IEnumerable<string> LastReadOrder() => _reads
        .Select((read, index) => (Label: read[..read.LastIndexOf(':')], Index: index))
        .GroupBy(_ => _.Label)
        .OrderBy(_ => _.Max(read => read.Index))
        .Select(_ => _.Key);

    IReadOnlyList<T> Observe<T>(string label, IReadOnlyList<T> items) =>
        new given.an_observed_list<T>(items, read => _reads.Add($"{label}:{read}"));
}
