// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_specifications_to_fixed_source : given.a_specification_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var text = new ValueSpec("name", SpecificationValueKind.Text, "Alice");
        StepSpec when = new(SpecificationStepPhase.When, SpecificationStepKind.Command, _command, Values: [text]);
        StepSpec then = new(SpecificationStepPhase.Then, SpecificationStepKind.Event, _event, Values: [text]);
        StepSpec given = new(SpecificationStepPhase.Given, SpecificationStepKind.Event, _event, Values: [text]);
        List<GenerationFact> facts =
        [
            .. BankingArtifacts(),
            .. Scenario(
                "V01",
                _command,
                [
                    given,
                    new(SpecificationStepPhase.Given, SpecificationStepKind.Event, _event, Values: [new("count", SpecificationValueKind.Number, "3"), new("flag", SpecificationValueKind.Boolean, "true"), new("note", SpecificationValueKind.Null)]),
                    new(SpecificationStepPhase.Given, SpecificationStepKind.ReadModel, _readModel, Values: [text]),
                    when,
                    then,
                    new(SpecificationStepPhase.Then, SpecificationStepKind.ReadModel, _readModel, Values: [text]),
                    new(SpecificationStepPhase.Then, SpecificationStepKind.Read, _query, Values: [text]),
                    new(SpecificationStepPhase.Then, SpecificationStepKind.Error, null, ErrorMessage: "Rejected")
                ]),
            .. Scenario(
                "V03",
                _command,
                [
                    given,
                    new(SpecificationStepPhase.Given, SpecificationStepKind.ReadModel, _readModel, Values: [text]),
                    new(SpecificationStepPhase.When, SpecificationStepKind.Command, _command, Values: [new("count", SpecificationValueKind.Number, "3"), new("flag", SpecificationValueKind.Boolean, "true")]),
                    then,
                    new(SpecificationStepPhase.Then, SpecificationStepKind.ReadModel, _readModel, Values: [text]),
                    new(SpecificationStepPhase.Then, SpecificationStepKind.Error, null, ErrorMessage: "Rejected")
                ]),
            .. Scenario(
                "V04",
                _command,
                [
                    when,
                    new(SpecificationStepPhase.Then, SpecificationStepKind.Read, _query, Values: [text])
                ]),
            .. Scenario("V02", _command, [when, new(SpecificationStepPhase.Then, SpecificationStepKind.Error, null)]),
            .. Scenario("U01", _command, [new(SpecificationStepPhase.When, SpecificationStepKind.Command, _command, Values: [new("tags", SpecificationValueKind.Collection, null, [new("first", SpecificationValueKind.Text, "a")])]), then]),
            .. Scenario("U02", _command, [when, new(SpecificationStepPhase.Then, SpecificationStepKind.Error, null, ErrorCode: "E1", ErrorMessage: "Nope")]),
            .. Scenario("U03", _command, [new(SpecificationStepPhase.When, SpecificationStepKind.Command, _command, Values: [new("nested.name", SpecificationValueKind.Text, "x", PathDepth: 2)]), then]),
            .. Scenario("U04", _command, [new(SpecificationStepPhase.Given, SpecificationStepKind.Read, _query), when, then]),
            .. Scenario("U05", _command, [when, new(SpecificationStepPhase.Then, SpecificationStepKind.Command, _command)]),
            .. Scenario("U06", _command, [new(SpecificationStepPhase.When, SpecificationStepKind.Event, _event), then])
        ];

        _result = Generator.Generate(Snapshot(Completed(Adapter, [.. facts])), new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact]
    void should_emit_the_complete_fixed_source() => _result.Source.ReplaceLineEndings("\n").ShouldEqual(
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

              specification ScenarioV02
                file Specs/V02.cs
                when RegisterAccount
                  name = "Alice"
                then error

              specification ScenarioV03
                file Specs/V03.cs
                given AccountRegistered
                  name = "Alice"
                given readmodel AccountView
                  name = "Alice"
                when RegisterAccount
                  count = 3
                  flag = true
                then AccountRegistered
                  name = "Alice"
                then readmodel AccountView
                  name = "Alice"
                then error "Rejected"

            slice StateView View

              query GetAccount => AccountView

              readmodel AccountView
                name String

        """.ReplaceLineEndings("\n"));

    [Fact]
    void should_report_the_complete_fixed_diagnostics() => Describe(_result.Diagnostics).ShouldEqual(
        """
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U01 | Specs/U01.cs:7:1-7:20 | Specification scenario 'ScenarioU01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U01 | Specs/U01.cs:7:1-7:20 | Specification scenario 'ScenarioU01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U01 | Specs/U01.cs:7:1-7:20 | Specification scenario 'ScenarioU01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U01 | Specs/U01.cs:7:1-7:20 | Specification scenario 'ScenarioU01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U01 | Specs/U01.cs:7:1-7:20 | Specification scenario 'ScenarioU01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U01 | Specs/U01.cs:7:1-7:20 | Specification scenario 'ScenarioU01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U01 | Specs/U01.cs:7:1-7:20 | Specification scenario 'ScenarioU01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U02 | Specs/U02.cs:7:1-7:20 | Specification scenario 'ScenarioU02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U02 | Specs/U02.cs:7:1-7:20 | Specification scenario 'ScenarioU02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U02 | Specs/U02.cs:7:1-7:20 | Specification scenario 'ScenarioU02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U02 | Specs/U02.cs:7:1-7:20 | Specification scenario 'ScenarioU02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U02 | Specs/U02.cs:7:1-7:20 | Specification scenario 'ScenarioU02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U03 | Specs/U03.cs:7:1-7:20 | Specification scenario 'ScenarioU03' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U03 | Specs/U03.cs:7:1-7:20 | Specification scenario 'ScenarioU03' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U03 | Specs/U03.cs:7:1-7:20 | Specification scenario 'ScenarioU03' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U03 | Specs/U03.cs:7:1-7:20 | Specification scenario 'ScenarioU03' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U03 | Specs/U03.cs:7:1-7:20 | Specification scenario 'ScenarioU03' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U03 | Specs/U03.cs:7:1-7:20 | Specification scenario 'ScenarioU03' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U04 | Specs/U04.cs:7:1-7:20 | Specification scenario 'ScenarioU04' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U04 | Specs/U04.cs:7:1-7:20 | Specification scenario 'ScenarioU04' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U04 | Specs/U04.cs:7:1-7:20 | Specification scenario 'ScenarioU04' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U04 | Specs/U04.cs:7:1-7:20 | Specification scenario 'ScenarioU04' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U04 | Specs/U04.cs:7:1-7:20 | Specification scenario 'ScenarioU04' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U04 | Specs/U04.cs:7:1-7:20 | Specification scenario 'ScenarioU04' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U04 | Specs/U04.cs:7:1-7:20 | Specification scenario 'ScenarioU04' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U05 | Specs/U05.cs:7:1-7:20 | Specification scenario 'ScenarioU05' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U05 | Specs/U05.cs:7:1-7:20 | Specification scenario 'ScenarioU05' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U05 | Specs/U05.cs:7:1-7:20 | Specification scenario 'ScenarioU05' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U05 | Specs/U05.cs:7:1-7:20 | Specification scenario 'ScenarioU05' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U05 | Specs/U05.cs:7:1-7:20 | Specification scenario 'ScenarioU05' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U06 | Specs/U06.cs:7:1-7:20 | Specification scenario 'ScenarioU06' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U06 | Specs/U06.cs:7:1-7:20 | Specification scenario 'ScenarioU06' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U06 | Specs/U06.cs:7:1-7:20 | Specification scenario 'ScenarioU06' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U06 | Specs/U06.cs:7:1-7:20 | Specification scenario 'ScenarioU06' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.U06 | Specs/U06.cs:7:1-7:20 | Specification scenario 'ScenarioU06' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V01 | Specs/V01.cs:7:1-7:20 | Specification scenario 'ScenarioV01' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V04 | Specs/V04.cs:7:1-7:20 | Specification scenario 'ScenarioV04' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V04 | Specs/V04.cs:7:1-7:20 | Specification scenario 'ScenarioV04' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V04 | Specs/V04.cs:7:1-7:20 | Specification scenario 'ScenarioV04' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V04 | Specs/V04.cs:7:1-7:20 | Specification scenario 'ScenarioV04' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V04 | Specs/V04.cs:7:1-7:20 | Specification scenario 'ScenarioV04' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.V04 | Specs/V04.cs:7:1-7:20 | Specification scenario 'ScenarioV04' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        """);
}
