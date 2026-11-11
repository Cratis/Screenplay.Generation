// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_admitting_values_with_a_late_invalid_child : given.a_specification_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var text = new ValueSpec("name", SpecificationValueKind.Text, "Alice");
        StepSpec when = new(SpecificationStepPhase.When, SpecificationStepKind.Command, _command, Values: [text]);
        List<GenerationFact> facts =
        [
            .. BankingArtifacts(),
            .. Scenario("C01", _command, [when, ThenWith(Composite("address", Text("street", "Main"), new ValueSpec("flag", SpecificationValueKind.Boolean, "yes")))]),
            .. Scenario("C02", _command, [when, ThenWith(Composite("address", Text("street", "Main"), Text("city", "Oslo")))]),
            .. Scenario("C03", _command, [when, ThenWith(Composite("address", new ValueSpec("flag", SpecificationValueKind.Boolean, "yes"), new ValueSpec("count", SpecificationValueKind.Number)))]),
            .. Scenario("C04", _command, [when, ThenWith(Composite("address", Text("street", "Main"), Composite("inner", Text("zip", "1"), new ValueSpec("zone", SpecificationValueKind.Null, "x"))))])
        ];

        _result = Generator.Generate(Snapshot(Completed(Adapter, [.. facts])), new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact]
    void should_emit_the_fixed_source() => _result.Source.ReplaceLineEndings("\n").ShouldEqual(
        """
        domain Banking

        module Accounts

          feature Opening

            slice StateChange Register

              command RegisterAccount
                name String
                count Int
                flag Bool

              event AccountRegistered
                name String

            slice StateView View

              query GetAccount => AccountView

              readmodel AccountView
                name String

        """.ReplaceLineEndings("\n"));

    [Fact]
    void should_report_the_complete_fixed_diagnostics() => Describe(_result.Diagnostics).ShouldEqual(
        """
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C01 | Specs/C01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C01 | Specs/C01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C01 | Specs/C01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C01 | Specs/C01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C01 | Specs/C01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C01 | Specs/C01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C01 | Specs/C01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C01 | Specs/C01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C03 | Specs/C03.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C03' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C03 | Specs/C03.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C03' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C03 | Specs/C03.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C03' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C03 | Specs/C03.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C03' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C03 | Specs/C03.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C03' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C03 | Specs/C03.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C03' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C03 | Specs/C03.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C03' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C03 | Specs/C03.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C03' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C04 | Specs/C04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C04 | Specs/C04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C04 | Specs/C04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C04 | Specs/C04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C04 | Specs/C04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C04 | Specs/C04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C04 | Specs/C04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C04 | Specs/C04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C04 | Specs/C04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.C04 | Specs/C04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.C04' could not be represented completely; no partial scenario was admitted
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.C02 | Specs/C02.cs:7:1-7:20 | Specification scenario 'ScenarioC02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.C02 | Specs/C02.cs:7:1-7:20 | Specification scenario 'ScenarioC02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.C02 | Specs/C02.cs:7:1-7:20 | Specification scenario 'ScenarioC02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.C02 | Specs/C02.cs:7:1-7:20 | Specification scenario 'ScenarioC02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.C02 | Specs/C02.cs:7:1-7:20 | Specification scenario 'ScenarioC02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.C02 | Specs/C02.cs:7:1-7:20 | Specification scenario 'ScenarioC02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.C02 | Specs/C02.cs:7:1-7:20 | Specification scenario 'ScenarioC02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.C02 | Specs/C02.cs:7:1-7:20 | Specification scenario 'ScenarioC02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        """);

    StepSpec ThenWith(ValueSpec value) => new(SpecificationStepPhase.Then, SpecificationStepKind.Event, _event, Values: [value]);

    static ValueSpec Text(string name, string scalar) => new(name, SpecificationValueKind.Text, scalar);

    static ValueSpec Composite(string name, params ValueSpec[] children) => new(name, SpecificationValueKind.Composite, null, children);
}
