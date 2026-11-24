// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_modern_and_legacy_inputs_select_the_same_member : given.role_candidates
{
    GeneratedScreenplayDefinition _modern = null!;
    GeneratedScreenplayDefinition _legacy = null!;
    GenerationFactLineage[] _legacyLineages = null!;
    void Because()
    {
        GenerationFact[] facts = [Candidate("first:earlier", "firstId", 1), Candidate("second:later", "secondId", 2, producer: Second)];
        GenerateBoth(facts);
        _modern = Generator.Generate(Modern(facts), new() { Domain = "Ordering" });
        _legacy = Generator.Generate(Inputs(facts), new() { Domain = "Ordering" });
        _legacyLineages = [.. GenerationFactDerivation.Derive(Capture(Inputs(facts))).Facts.Where(record => record.Fact is ArtifactMemberRoleFact).Select(record => record.Lineage!)];
    }
    [Fact] void should_generate_successfully() => _modern.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_the_same_source() => _modern.Source.ShouldEqual(_legacy.Source);
    [Fact] void should_have_the_same_graph() => System.Text.Json.JsonSerializer.Serialize(_modern.Graph).ShouldEqual(System.Text.Json.JsonSerializer.Serialize(_legacy.Graph));
    [Fact] void should_have_the_same_selected_role_lineage() => AdapterRunProjection(_modern.AdapterRun.Derivation!.Facts.Where(record => record.Fact is ArtifactMemberRoleFact).Select(record => record.Lineage).ToArray()).ShouldEqual(AdapterRunProjection(_legacyLineages));
    [Fact] void should_record_the_selection_rule() => _modern.AdapterRun.Derivation!.Rules.Select(rule => rule.Rule.Id).ShouldContain(given.selection_result.Rule.Id);
    [Fact] void should_lower_the_derived_role() => _modern.AdapterRun.Derivation!.Facts.Single(record => record.Fact is ArtifactMemberRoleFact).Disposition.ShouldEqual(GenerationFactDisposition.Lowered);
}
