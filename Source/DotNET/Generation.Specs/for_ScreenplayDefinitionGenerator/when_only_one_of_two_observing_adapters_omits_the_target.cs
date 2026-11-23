// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_only_one_of_two_observing_adapters_omits_the_target : given.an_omitted_type_use
{
    GeneratedScreenplayDefinition _result = null!;
    FactId _otherTypeUse = null!;

    void Because()
    {
        var facts = Facts();
        var other = new AdapterIdentity { Id = "other", Version = "1.0.0" };
        _otherTypeUse = new FactId { Value = "other:type-use" };
        var typeUse = facts.OfType<ArtifactMemberTypeUseFact>().Single();
        var otherTypeUse = typeUse with
        {
            Id = _otherTypeUse,
            Evidence = typeUse.Evidence with { Adapter = other }
        };
        _result = Generator.Generate(
            Snapshot(Completed(Adapter, facts), Completed(other, [otherTypeUse])),
            new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_keep_the_missing_target_error() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).ShouldBeTrue();
    [Fact] void should_not_waive_the_incomplete_member_error() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.IncompleteArtifactMember).ShouldBeTrue();
    [Fact] void should_include_the_other_adapters_observation_in_the_missing_target_diagnostic() => _result.AdapterRun.Derivation.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).Facts.ShouldContain(_otherTypeUse);
    [Fact] void should_not_claim_the_member_target_was_omitted() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "example.FlagsEnumOmitted").ShouldBeFalse();
    [Fact] void should_not_generate_successfully() => _result.IsSuccess.ShouldBeFalse();
}
