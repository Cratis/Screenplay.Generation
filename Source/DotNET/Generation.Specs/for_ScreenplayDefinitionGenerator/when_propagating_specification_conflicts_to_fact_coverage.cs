// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_propagating_specification_conflicts_to_fact_coverage : given.a_coverage_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var scenario = Scenario(
            "S1",
            _command,
            [new(SpecificationStepPhase.When, SpecificationStepKind.Command, _command, Values: [new("name", SpecificationValueKind.Text, "Cratis")])]);
        var scenarioFact = scenario.OfType<SpecificationScenarioFact>().Single();
        var stepFact = scenario.OfType<SpecificationStepFact>().Single();
        var valueFact = scenario.OfType<SpecificationValueFact>().Single();

        _result = Run([
            .. BankingArtifacts(),
            .. scenario,
            scenarioFact with
            {
                Id = new FactId { Value = "spec:S1:other" },
                Definition = scenarioFact.Definition with { Name = "OtherScenario" }
            },
            stepFact with
            {
                Id = new FactId { Value = "spec:S1:step:other" },
                Definition = stepFact.Definition with { Phase = SpecificationStepPhase.Then }
            },
            valueFact with
            {
                Id = new FactId { Value = "spec:S1:value:other" },
                Definition = valueFact.Definition with { Scalar = "Other" }
            }]);
    }

    [Fact]
    void should_associate_the_scenario_step_and_value_conflicts() => Records(_result).ShouldEqual(
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
        spec:S1|Conflicted|GEN0033|Conflict|Specification scenario 'dotnet://Banking/Specs.S1' has 2 incompatible definitions
        spec:S1:other|Conflicted|GEN0033|Conflict|Specification scenario 'dotnet://Banking/Specs.S1' has 2 incompatible definitions
        spec:S1:step:0|Conflicted|GEN0034|Conflict|Specification step '0' in 'dotnet://Banking/Specs.S1' has 2 incompatible definitions
        spec:S1:step:other|Conflicted|GEN0034|Conflict|Specification step '0' in 'dotnet://Banking/Specs.S1' has 2 incompatible definitions
        spec:S1:value:0:name|Conflicted|GEN0035|Conflict|Specification value 'name' in step '0' has 2 incompatible definitions
        spec:S1:value:other|Conflicted|GEN0035|Conflict|Specification value 'name' in step '0' has 2 incompatible definitions
        """);
}
