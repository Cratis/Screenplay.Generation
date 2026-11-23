// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.when_binding_concept_only_type_uses;

public class with_raw_contributions_reusing_the_fact_id : given.concept_only_type_uses
{
    AdapterContribution[] _contributions;
    GeneratedScreenplayDefinition _result;

    void Establish()
    {
        var own = OptedCommand();
        var observation = own.OfType<ArtifactMemberTypeUseFact>().Single();
        var foreign = observation with { Evidence = observation.Evidence with { Adapter = new() { Id = "foreign", Version = "1" } } };
        _contributions =
        [
            new() { Adapter = own[0].Evidence.Adapter, Facts = own },
            new() { Adapter = foreign.Evidence.Adapter, Facts = [foreign] }
        ];
    }

    void Because() => _result = Generator.Generate(_contributions, new() { Domain = "Ordering" });

    [Fact] void should_fail_generation() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_the_strict_missing_target_error() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).ShouldBeTrue();
    [Fact] void should_locate_the_strict_observation() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).Source.ShouldEqual(_contributions[1].Facts.Single().Evidence.Source);
    [Fact] void should_not_emit_the_incomplete_command() => _result.Source.ShouldNotContain("command SetPermissions");
}
