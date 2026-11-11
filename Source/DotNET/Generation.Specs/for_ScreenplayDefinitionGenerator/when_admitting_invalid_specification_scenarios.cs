// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_admitting_invalid_specification_scenarios : given.a_specification_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var text = new ValueSpec("name", SpecificationValueKind.Text, "Alice");
        StepSpec when = new(SpecificationStepPhase.When, SpecificationStepKind.Command, _command, Values: [text]);
        StepSpec then = new(SpecificationStepPhase.Then, SpecificationStepKind.Event, _event, Values: [text]);
        StepSpec given = new(SpecificationStepPhase.Given, SpecificationStepKind.Event, _event, Values: [text]);
        StepSpec[] basic = [when, then];
        var ghost = new ArtifactKey { Subject = new SubjectId { Value = "dotnet://Banking/Ghost" }, Kind = ArtifactKind.Command };
        var unplaced = new ArtifactKey { Subject = new SubjectId { Value = "dotnet://Banking/Unplaced" }, Kind = ArtifactKind.Command };
        List<GenerationFact> facts =
        [
            .. BankingArtifacts(),
            .. Artifact(ArtifactKind.Command, "Unplaced", "Register", GenerationSliceKind.StateChange, 5).Take(1),
            .. Scenario("A01", ghost, basic),
            .. Scenario("A02", _command, []),
            .. Tweak<SpecificationScenarioFact>(Scenario("A03", _command, basic), _ => _ with { Definition = _.Definition with { Steps = [_.Definition.Steps[0], _.Definition.Steps[0]] } }),
            .. Tweak<SpecificationScenarioFact>(Scenario("A04", _command, basic), _ => _ with { Definition = _.Definition with { Steps = [_.Definition.Steps[1], _.Definition.Steps[0]] } }),
            .. Scenario("A05", _command, basic).Where(_ => _.Id.Value != "spec:A05:step:1"),
            .. Scenario("A06", _command, [when, new(SpecificationStepPhase.Then, SpecificationStepKind.Event, _command)]),
            .. Scenario("A07", _command, [when, new(SpecificationStepPhase.Then, SpecificationStepKind.Error, _event)]),
            .. Scenario("A08", _command, [new(SpecificationStepPhase.Given, SpecificationStepKind.Error, null), when, then]),
            .. Scenario("A09", _command, [when, new(SpecificationStepPhase.Then, SpecificationStepKind.Event, _event, ErrorMessage: "Stray")]),
            .. Scenario("A10", _command, [when, when, then]),
            .. Scenario("A11", _command, [given, when]),
            .. Scenario("A12", _command, [then, given]),
            .. Tweak<SpecificationStepFact>(Scenario("A13", _command, basic), _ => _ with { Definition = _.Definition with { Values = [_.Definition.Values[0], _.Definition.Values[0]] } }),
            .. Scenario("A14", _command, basic).Where(_ => _.Id.Value != "spec:A14:value:1:name"),
            .. Scenario("A15", _command, [when, new(SpecificationStepPhase.Then, SpecificationStepKind.Event, _event, Values: [new("name", SpecificationValueKind.Text)])]),
            .. Scenario("A16", _command, [when, new(SpecificationStepPhase.Then, SpecificationStepKind.Event, _event, Values: [new("flag", SpecificationValueKind.Boolean, "yes")])]),
            .. Scenario("A17", _command, [when, new(SpecificationStepPhase.Then, SpecificationStepKind.Event, _event, Values: [new("loop", SpecificationValueKind.Composite, null, [new("loop", SpecificationValueKind.Text, "x", SelfPath: true)])])]),
            .. Scenario("A18", _command, [when, new(SpecificationStepPhase.Then, SpecificationStepKind.Event, _event, Values: [new("name", SpecificationValueKind.Text, "x", OtherStep: 0)])]),
            .. Scenario("A19", unplaced, basic)
        ];

        _result = Generator.Generate(Snapshot(Completed(Adapter, [.. facts])), new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact]
    void should_emit_only_the_unaffected_source() => _result.Source.ReplaceLineEndings("\n").ShouldEqual(
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
        GEN0009 | Warning | Unknown | dotnet://Banking/Unplaced | Accounts/Register/Unplaced.cs:5:1-5:20 | The recognized Command artifact 'Unplaced' has no Screenplay placement and was omitted
        GEN0009 | Warning | Unknown | dotnet://Banking/Unplaced | Accounts/Register/Unplaced.cs:5:1-5:20 | The recognized Command artifact 'Unplaced' has no Screenplay placement and was omitted
        GEN0040 | Warning | Unsupported | dotnet://Banking/Specs.A03 | Specs/A03.cs:9:1-9:20 | Admitted specification step fact 'spec:A03:step:1' did not contribute to emitted Screenplay syntax and was omitted
        GEN0040 | Warning | Unsupported | dotnet://Banking/Specs.A03 | Specs/A03.cs:21:1-21:20 | Admitted specification value fact 'spec:A03:value:1:name' did not contribute to emitted Screenplay syntax and was omitted
        GEN0040 | Warning | Unsupported | dotnet://Banking/Specs.A05 | Specs/A05.cs:21:1-21:20 | Admitted specification value fact 'spec:A05:value:1:name' did not contribute to emitted Screenplay syntax and was omitted
        GEN0035 | Error | Conflict | dotnet://Banking/Specs.A18 | Specs/A18.cs:20:1-20:20 | Specification value 'name' in step '0' has 2 incompatible definitions
        GEN0035 | Error | Conflict | dotnet://Banking/Specs.A18 | Specs/A18.cs:20:1-20:20 | Specification value 'name' in step '0' has 2 incompatible definitions
        GEN0035 | Error | Conflict | dotnet://Banking/Specs.A18 | Specs/A18.cs:20:1-20:20 | Specification value 'name' in step '0' has 2 incompatible definitions
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A01 | Specs/A01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A01 | Specs/A01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A01 | Specs/A01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A01 | Specs/A01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A01 | Specs/A01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A01 | Specs/A01.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A01' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A02 | Specs/A02.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A02' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A02 | Specs/A02.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A02' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A03 | Specs/A03.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A03' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A03 | Specs/A03.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A03' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A03 | Specs/A03.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A03' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A03 | Specs/A03.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A03' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A04 | Specs/A04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A04 | Specs/A04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A04 | Specs/A04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A04 | Specs/A04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A04 | Specs/A04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A04 | Specs/A04.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A04' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A05 | Specs/A05.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A05' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A05 | Specs/A05.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A05' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A05 | Specs/A05.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A05' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A05 | Specs/A05.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A05' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A06 | Specs/A06.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A06' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A06 | Specs/A06.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A06' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A06 | Specs/A06.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A06' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A06 | Specs/A06.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A06' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A06 | Specs/A06.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A06' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A07 | Specs/A07.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A07' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A07 | Specs/A07.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A07' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A07 | Specs/A07.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A07' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A07 | Specs/A07.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A07' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A07 | Specs/A07.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A07' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A08 | Specs/A08.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A08' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A08 | Specs/A08.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A08' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A08 | Specs/A08.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A08' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A08 | Specs/A08.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A08' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A08 | Specs/A08.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A08' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A08 | Specs/A08.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A08' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A08 | Specs/A08.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A08' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A09 | Specs/A09.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A09' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A09 | Specs/A09.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A09' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A09 | Specs/A09.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A09' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A09 | Specs/A09.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A09' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A09 | Specs/A09.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A09' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A10 | Specs/A10.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A10' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A10 | Specs/A10.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A10' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A10 | Specs/A10.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A10' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A10 | Specs/A10.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A10' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A10 | Specs/A10.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A10' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A10 | Specs/A10.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A10' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A10 | Specs/A10.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A10' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A10 | Specs/A10.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A10' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A11 | Specs/A11.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A11' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A11 | Specs/A11.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A11' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A11 | Specs/A11.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A11' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A11 | Specs/A11.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A11' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A11 | Specs/A11.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A11' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A11 | Specs/A11.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A11' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A12 | Specs/A12.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A12' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A12 | Specs/A12.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A12' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A12 | Specs/A12.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A12' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A12 | Specs/A12.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A12' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A12 | Specs/A12.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A12' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A12 | Specs/A12.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A12' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A13 | Specs/A13.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A13' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A13 | Specs/A13.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A13' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A13 | Specs/A13.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A13' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A13 | Specs/A13.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A13' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A13 | Specs/A13.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A13' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A13 | Specs/A13.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A13' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A14 | Specs/A14.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A14' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A14 | Specs/A14.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A14' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A14 | Specs/A14.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A14' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A14 | Specs/A14.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A14' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A14 | Specs/A14.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A14' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A15 | Specs/A15.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A15' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A15 | Specs/A15.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A15' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A15 | Specs/A15.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A15' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A15 | Specs/A15.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A15' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A15 | Specs/A15.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A15' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A15 | Specs/A15.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A15' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A16 | Specs/A16.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A16' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A16 | Specs/A16.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A16' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A16 | Specs/A16.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A16' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A16 | Specs/A16.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A16' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A16 | Specs/A16.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A16' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A16 | Specs/A16.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A16' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A17 | Specs/A17.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A17' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A17 | Specs/A17.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A17' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A17 | Specs/A17.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A17' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A17 | Specs/A17.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A17' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A17 | Specs/A17.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A17' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A17 | Specs/A17.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A17' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A18 | Specs/A18.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A18' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A18 | Specs/A18.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A18' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A18 | Specs/A18.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A18' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A18 | Specs/A18.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A18' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A19 | Specs/A19.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A19' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A19 | Specs/A19.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A19' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A19 | Specs/A19.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A19' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A19 | Specs/A19.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A19' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A19 | Specs/A19.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A19' could not be represented completely; no partial scenario was admitted
        GEN0037 | Error | Unsupported | dotnet://Banking/Specs.A19 | Specs/A19.cs:7:1-7:20 | Specification scenario 'dotnet://Banking/Specs.A19' could not be represented completely; no partial scenario was admitted
        """);
}
