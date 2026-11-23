// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_adapters_reuse_projected_observation_ids : given.compatibility_type_uses
{
    GeneratedScreenplayDefinition _raw = null!;
    GeneratedScreenplayDefinition _snapshot = null!;
    GeneratedScreenplayDefinition _reverse = null!;
    ArtifactMemberTypeUseFact _foreign = null!;

    void Because()
    {
        var own = ProjectedCommand();
        var observation = own.OfType<ArtifactMemberTypeUseFact>().Single();
        var evidence = observation.Evidence with { Adapter = new() { Id = "foreign", Version = "1" } };
        var foreignArtifact = observation.Definition.Member.Artifact with { Kind = ArtifactKind.ReadModel };
        var member = observation.Definition.Member with { Artifact = foreignArtifact, Name = "strict" };
        _foreign = observation with { Evidence = evidence, Definition = observation.Definition with { Member = member } };
        var declaration = own.OfType<ArtifactMemberDeclarationFact>().Single();
        var owner = own.OfType<ArtifactDeclarationFact>().Single();
        var placement = own.OfType<ArtifactPlacementFact>().Single();
        GenerationFact[] foreign =
        [
            owner with { Id = Id("foreign-owner"), Evidence = evidence, Definition = owner.Definition with { Artifact = foreignArtifact, Name = "ForeignRecord" } },
            placement with { Id = Id("foreign-placement"), Evidence = evidence, Artifact = foreignArtifact, Placement = placement.Placement with { Slice = "ForeignRecord", SliceKind = GenerationSliceKind.StateView } },
            declaration with { Id = Id("strict-member"), Evidence = evidence, Definition = declaration.Definition with { Member = member, DeclarationOrder = 0 } },
            _foreign
        ];
        var ownFacts = own.Concat(Candidate("enum", false)).ToArray();
        _raw = Generator.Generate([new AdapterContribution { Adapter = Adapter, Facts = ownFacts }, new AdapterContribution { Adapter = evidence.Adapter, Facts = foreign }], new() { Domain = "Ordering" });
        _snapshot = GenerateAdapters(ownFacts, foreign);
        _reverse = GenerateAdapters(ownFacts, foreign, true);
    }

    [Fact] void should_keep_the_raw_projected_member() => _raw.Source.ShouldContain("permissions List[]");
    [Fact] void should_keep_the_snapshot_projected_member() => _snapshot.Source.ShouldContain("permissions List[]");
    [Fact] void should_keep_the_foreign_nested_shape_strict() => _raw.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.UnsupportedTypeUseShape).ShouldBeTrue();
    [Fact] void should_keep_the_snapshot_foreign_shape_strict() => _snapshot.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.UnsupportedTypeUseShape).ShouldBeTrue();
    [Fact] void should_not_print_the_foreign_nested_member() => _raw.Source.Contains("strict List[]", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_bind_the_foreign_terminal_only() => _snapshot.AdapterRun!.Derivation.Facts.Select(record => record.Fact).OfType<TypeUseBindingFact>().Single().Definition.Member.Name.ShouldEqual("strict");
    [Fact] void should_keep_both_reused_identifiers() => _snapshot.AdapterRun!.Facts.Count(record => record.Fact is ArtifactMemberTypeUseFact && record.Fact.Id == _foreign.Id).ShouldEqual(2);
    [Fact] void should_keep_foreign_observation_evidence() => Projection(_snapshot.AdapterRun!.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single(fact => fact.Evidence.Adapter == _foreign.Evidence.Adapter)).ShouldEqual(Projection(_foreign));
    [Fact] void should_keep_raw_and_snapshot_source_equal() => _raw.Source.ShouldEqual(_snapshot.Source);
    [Fact] void should_keep_reversed_adapter_source_equal() => _reverse.Source.ShouldEqual(_snapshot.Source);
    [Fact] void should_keep_reversed_adapter_diagnostics_equal() => Projection(_reverse.Diagnostics).ShouldEqual(Projection(_snapshot.Diagnostics));
    [Fact] void should_keep_reversed_adapter_lineage_equal() => Projection(_reverse.AdapterRun!.Derivation).ShouldEqual(Projection(_snapshot.AdapterRun!.Derivation));
}
