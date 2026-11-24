// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_generating_legacy_artifact_execution_probes : given.input_only_execution
{
    GeneratedScreenplayDefinition[] _forward;
    GeneratedScreenplayDefinition[] _reverse;

    void Because()
    {
        var facts = InputFacts(Place(Transfer().Definition.Artifact), Place(Document), ReturnDocument());
        var cases = new[]
        {
            facts.Where(fact => fact is not (MethodArtifactUsageFact or ArtifactUsageDeclarationFact)),
            facts.Where(fact => fact is not ArtifactUsageDeclarationFact),
            facts.AsEnumerable()
        };
        var generator = new ScreenplayDefinitionGenerator();
        var options = new ScreenplayGenerationOptions { Domain = "Application" };
        _forward = [.. cases.Select(facts => generator.Generate(Run([.. facts]), options))];
        _reverse = [.. cases.Select(facts => generator.Generate(Run([.. facts.Reverse()]), options))];
    }

    [Fact] void should_not_transfer_without_execution() => _forward[0].Graph.Artifacts.Single(artifact => artifact.Key == Transfer().Definition.Artifact).Variants.Single().Definition.Properties.ShouldBeEmpty();
    [Fact] void should_still_transfer_without_an_artifact_mapping() => _forward[1].Source.ShouldContain("filter string");
    [Fact] void should_preserve_the_unmapped_artifact_usage_error() => _forward[1].Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingArtifactUsageMapping).ShouldBeTrue();
    [Fact] void should_not_invent_a_relationship_for_an_unmapped_usage() => _forward[1].Graph.Relationships.Any(relationship => relationship.Key.Kind == RelationshipKind.Reads).ShouldBeFalse();
    [Fact] void should_compile_the_mapped_artifact_usage() => _forward[2].IsSuccess.ShouldBeTrue();
    [Fact] void should_transfer_the_mapped_declaration_inputs() => _forward[2].Source.ShouldContain("filter string");
    [Fact] void should_preserve_the_mapped_reads_relationship() => _forward[2].Graph.Relationships.Count(relationship => relationship.Key.Kind == RelationshipKind.Reads).ShouldEqual(1);
    [Fact] void should_preserve_graphs_and_diagnostics_under_permutation() => Projection(_forward.Select(result => result.Graph)).ShouldEqual(Projection(_reverse.Select(result => result.Graph)));
    [Fact] void should_preserve_all_lineage_under_permutation() => Projection(_forward.Select(result => result.AdapterRun)).ShouldEqual(Projection(_reverse.Select(result => result.AdapterRun)));
    [Fact] void should_preserve_source_under_permutation() => _forward.Select(result => result.Source).ShouldEqual(_reverse.Select(result => result.Source));
}
