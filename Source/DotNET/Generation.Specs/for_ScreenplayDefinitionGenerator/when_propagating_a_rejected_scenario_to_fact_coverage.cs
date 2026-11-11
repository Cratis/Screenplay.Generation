// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_propagating_a_rejected_scenario_to_fact_coverage : given.a_coverage_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var scenario = Scenario(
            "S2",
            _command,
            [
                new(SpecificationStepPhase.When, SpecificationStepKind.Command, _command, Values: [new("name", SpecificationValueKind.Text, "Cratis")]),
                new(
                    SpecificationStepPhase.Then,
                    SpecificationStepKind.Event,
                    _event,
                    Values:
                    [
                        new("detail", SpecificationValueKind.Composite, Children: [new("part", SpecificationValueKind.Text, "x")]),
                        new("name", SpecificationValueKind.Text, "Cratis")
                    ])
            ]);

        _result = Run([
            .. BankingArtifacts(),
            .. scenario.Where(fact => fact.Id.Value != "spec:S2:value:0:name")]);
    }

    [Fact]
    void should_omit_the_scenario_its_steps_and_their_values() => Records(_result).ShouldEqual(
        """
        artifact:AccountRegistered|Lowered|
        artifact:AccountView|Lowered|
        artifact:GetAccount|Lowered|
        artifact:RegisterAccount|Lowered|
        placement:AccountRegistered|Lowered|
        placement:AccountView|Lowered|
        placement:GetAccount|Lowered|
        placement:RegisterAccount|Lowered|
        relationship:Returns:GetAccount:AccountView|Lowered|
        spec:S2|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S2' could not be represented completely; no partial scenario was admitted
        spec:S2:step:0|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S2' could not be represented completely; no partial scenario was admitted
        spec:S2:step:1|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S2' could not be represented completely; no partial scenario was admitted
        spec:S2:value:1:detail|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S2' could not be represented completely; no partial scenario was admitted
        spec:S2:value:1:detail.part|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S2' could not be represented completely; no partial scenario was admitted
        spec:S2:value:1:name|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S2' could not be represented completely; no partial scenario was admitted
        """);
}
