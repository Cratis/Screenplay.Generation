// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_omitting_shared_and_cyclic_values : given.a_coverage_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var scenario = Scenario(
            "S3",
            _command,
            [
                new(SpecificationStepPhase.When, SpecificationStepKind.Command, _command, Values: [new("name", SpecificationValueKind.Text, "Cratis")]),
                new(
                    SpecificationStepPhase.Then,
                    SpecificationStepKind.Event,
                    _event,
                    Values:
                    [
                        new(
                            "alpha",
                            SpecificationValueKind.Composite,
                            Children:
                            [
                                new("one", SpecificationValueKind.Text, "1"),
                                new("missing", SpecificationValueKind.Text, "2")
                            ]),
                        new("beta", SpecificationValueKind.Composite, Children: [new("shared", SpecificationValueKind.Text, "3")]),
                        new("loop", SpecificationValueKind.Composite, Children: [new("loop", SpecificationValueKind.Text, "x", SelfPath: true)])
                    ])
            ]);
        var facts = scenario
            .Where(fact => fact.Id.Value != "spec:S3:value:1:alpha.missing")
            .Select(ShareDescendant);

        _result = Run([.. BankingArtifacts(), .. facts]);
    }

    [Fact]
    void should_omit_every_reachable_value_and_terminate() => Records(_result).ShouldEqual(
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
        spec:S3|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S3' could not be represented completely; no partial scenario was admitted
        spec:S3:step:0|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S3' could not be represented completely; no partial scenario was admitted
        spec:S3:step:1|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S3' could not be represented completely; no partial scenario was admitted
        spec:S3:value:0:name|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S3' could not be represented completely; no partial scenario was admitted
        spec:S3:value:1:alpha|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S3' could not be represented completely; no partial scenario was admitted
        spec:S3:value:1:alpha.one|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S3' could not be represented completely; no partial scenario was admitted
        spec:S3:value:1:beta|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S3' could not be represented completely; no partial scenario was admitted
        spec:S3:value:1:beta.shared|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S3' could not be represented completely; no partial scenario was admitted
        spec:S3:value:1:loop|OmittedWithDiagnostic|GEN0037|Unsupported|Specification scenario 'dotnet://Banking/Specs.S3' could not be represented completely; no partial scenario was admitted
        """);

    static GenerationFact ShareDescendant(GenerationFact fact) =>
        fact is SpecificationValueFact alpha && string.Equals(alpha.Id.Value, "spec:S3:value:1:alpha", StringComparison.Ordinal)
            ? alpha with
            {
                Definition = alpha.Definition with
                {
                    Children =
                    [
                        .. alpha.Definition.Children,
                        new SpecificationValueKey { Step = alpha.Definition.Key.Step, Path = ["beta", "shared"] }
                    ]
                }
            }
            : fact;
}
