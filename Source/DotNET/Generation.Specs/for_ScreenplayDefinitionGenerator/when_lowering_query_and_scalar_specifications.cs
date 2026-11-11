// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_query_and_scalar_specifications : given.a_specification_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var text = new ValueSpec("name", SpecificationValueKind.Text, "Alice");
        List<GenerationFact> facts =
        [
            .. BankingArtifacts(),
            .. Scenario(
                "Q01",
                _query,
                [
                    new(SpecificationStepPhase.Given, SpecificationStepKind.ReadModel, _readModel, Values: [text]),
                    new(
                        SpecificationStepPhase.Then,
                        SpecificationStepKind.Read,
                        _query,
                        Values:
                        [
                            new("id", SpecificationValueKind.Text, "7", Path: ["arguments", "id"]),
                            new("limit", SpecificationValueKind.Number, "5", Path: ["arguments", "limit"]),
                            new("name", SpecificationValueKind.Text, "Bob", Path: ["result", "1", "name"]),
                            new("owner", SpecificationValueKind.Text, "Eve", Path: ["result", "0", "owner"]),
                            new("name", SpecificationValueKind.Text, "Alice", Path: ["result", "0", "name"])
                        ])
                ]),
            .. Scenario(
                "Q02",
                _query,
                [new(SpecificationStepPhase.Then, SpecificationStepKind.Read, _query, Values: [new("name", SpecificationValueKind.Text, "Bob", Path: ["result", "x", "name"])])]),
            .. Scenario(
                "Q03",
                _query,
                [new(SpecificationStepPhase.Then, SpecificationStepKind.Read, _query, Values: [new("name", SpecificationValueKind.Text, "Bob", Path: ["other", "name"])])]),
            .. Scenario(
                "S01",
                _command,
                [
                    new(
                        SpecificationStepPhase.When,
                        SpecificationStepKind.Command,
                        _command,
                        Values:
                        [
                            new("flag", SpecificationValueKind.Boolean, "false"),
                            new("count", SpecificationValueKind.Number, "0")
                        ]),
                    new(SpecificationStepPhase.Then, SpecificationStepKind.Event, _event, Values: [new("name", SpecificationValueKind.Null)])
                ])
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

              specification ScenarioS01
                file Specs/S01.cs
                when RegisterAccount
                  flag = false
                  count = 0
                then AccountRegistered
                  name = null

            slice StateView View

              query GetAccount => AccountView

              readmodel AccountView
                name String

              specification ScenarioQ01
                file Specs/Q01.cs
                given readmodel AccountView
                  name = "Alice"
                then query GetAccount
                  arguments
                    id = "7"
                    limit = 5
                  result
                    owner = "Eve"
                    name = "Alice"
                  result
                    name = "Bob"

        """.ReplaceLineEndings("\n"));

    [Fact]
    void should_report_the_complete_fixed_diagnostics() => Describe(_result.Diagnostics).ShouldEqual(
        """
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.Q02 | Specs/Q02.cs:7:1-7:20 | Specification scenario 'ScenarioQ02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.Q02 | Specs/Q02.cs:7:1-7:20 | Specification scenario 'ScenarioQ02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.Q02 | Specs/Q02.cs:7:1-7:20 | Specification scenario 'ScenarioQ02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.Q02 | Specs/Q02.cs:7:1-7:20 | Specification scenario 'ScenarioQ02' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.Q03 | Specs/Q03.cs:7:1-7:20 | Specification scenario 'ScenarioQ03' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.Q03 | Specs/Q03.cs:7:1-7:20 | Specification scenario 'ScenarioQ03' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.Q03 | Specs/Q03.cs:7:1-7:20 | Specification scenario 'ScenarioQ03' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        GEN0038 | Error | Unsupported | dotnet://Banking/Specs.Q03 | Specs/Q03.cs:7:1-7:20 | Specification scenario 'ScenarioQ03' uses behavior the current Screenplay syntax cannot represent exactly and was omitted as a whole
        """);
}
