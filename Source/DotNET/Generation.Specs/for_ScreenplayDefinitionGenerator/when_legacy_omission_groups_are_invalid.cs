// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_legacy_omission_groups_are_invalid : given.omission_groups
{
    [Fact] void should_report_malformed_metadata_without_emitting_the_payload()
    {
        var member = Member("member");
        var group = Group(member) with { Definition = null! };
        AssertInvalid(Generate([member, group]), 1);
    }
    [Fact] void should_report_foreign_coverage_without_hiding_generic_noise()
    {
        var member = Member("member");
        var group = Group(member);
        AssertInvalid(Generate([member, group with { Definition = group.Definition with { CoveredFacts = [new() { Value = "foreign:member" }] } }]), 1);
    }
    [Fact] void should_refuse_overlapping_groups()
    {
        var member = Member("member");
        var group = Group(member);
        AssertInvalid(Generate([member, group, group with { Id = Id("overlap") }]), 2);
    }

    [Fact] void should_reject_ambiguous_group_identities()
    {
        var first = Member("first");
        var second = Member("second");
        AssertInvalid(Generate([first, second, Group(first), Group(second)]), 1);
    }
    [Fact] void should_report_missing_coverage_without_crashing()
    {
        var member = Member("member");
        var group = Group(member);
        AssertInvalid(Generate([member, group with { Definition = group.Definition with { CoveredFacts = null! } }]), 1);
    }

    static void AssertInvalid(GeneratedScreenplayDefinition result, int count)
    {
        result.Diagnostics.Count(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidOmissionDiagnosticGroup).ShouldEqual(count);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "OBS_GROUP").ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.OmittedGenerationFact).ShouldBeTrue();
        result.AdapterRun.Facts.Where(record => record.Fact is OmittedFactDiagnosticGroupFact).All(record => record.Disposition == GenerationFactDisposition.OmittedWithDiagnostic).ShouldBeTrue();
    }
}
