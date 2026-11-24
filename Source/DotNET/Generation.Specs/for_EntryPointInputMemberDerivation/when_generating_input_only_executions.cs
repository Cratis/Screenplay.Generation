// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_generating_input_only_executions : given.input_only_execution
{
    GeneratedScreenplayDefinition _forward;
    GeneratedScreenplayDefinition _reverse;
    GeneratedScreenplayDefinition _contributions;
    GeneratedScreenplayDefinition _reversedContributions;

    void Because()
    {
        var facts = InputOnlyFacts(Place(Transfer().Definition.Artifact), Place(Document), ReturnDocument());
        var generator = new ScreenplayDefinitionGenerator();
        var options = new ScreenplayGenerationOptions { Domain = "Application" };
        var contributions = facts.GroupBy(fact => fact.Evidence.Adapter).Select(group => new AdapterContribution { Adapter = group.Key, Facts = [.. group] }).ToArray();
        _forward = generator.Generate(Run(facts), options);
        _reverse = generator.Generate(Run([.. facts.Reverse()]), options);
        _contributions = generator.Generate(contributions, options);
        _reversedContributions = generator.Generate(contributions.Reverse().Select(contribution => contribution with { Facts = [.. contribution.Facts.Reverse()] }), options);
    }

    [Fact] void should_materialize_the_input() => _forward.Graph.Artifacts.Single(artifact => artifact.Key == Transfer().Definition.Artifact).Variants.Single().Definition.Properties.Single().Name.ShouldEqual("filter");
    [Fact] void should_compile_the_printed_input() => _forward.IsSuccess.ShouldBeTrue();
    [Fact] void should_print_the_input() => _forward.Source.ShouldContain("filter string");
    [Fact] void should_keep_the_execution_as_provenance() => _forward.AdapterRun.Facts.Single(record => record.Fact.Id == InputExecution().Id).Disposition.ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_not_invent_a_reads_relationship() => _forward.Graph.Relationships.Any(relationship => relationship.Key.Kind == RelationshipKind.Reads).ShouldBeFalse();
    [Fact] void should_not_invent_an_artifact_for_the_declaration() => _forward.Graph.Artifacts.Any(artifact => artifact.Key.Subject == Declaration).ShouldBeFalse();
    [Fact] void should_preserve_the_execution_in_member_lineage() => _forward.AdapterRun.Derivation.Facts.Single(record => record.Fact is ArtifactMemberDeclarationFact).Lineage.Inputs.ShouldContain(InputExecution().Id);
    [Fact] void should_preserve_all_lineage_under_permutation() => Projection(_forward.AdapterRun).ShouldEqual(Projection(_reverse.AdapterRun));
    [Fact] void should_preserve_graph_and_diagnostics_under_permutation() => Projection(_forward.Graph).ShouldEqual(Projection(_reverse.Graph));
    [Fact] void should_preserve_source_under_permutation() => _forward.Source.ShouldEqual(_reverse.Source);
    [Fact] void should_compile_contributions() => _contributions.IsSuccess.ShouldBeTrue();
    [Fact] void should_use_the_same_transfer_for_contributions() => _contributions.Source.ShouldEqual(_forward.Source);
    [Fact] void should_preserve_contribution_order_independence() => Projection(_contributions.AdapterRun).ShouldEqual(Projection(_reversedContributions.AdapterRun));
}
