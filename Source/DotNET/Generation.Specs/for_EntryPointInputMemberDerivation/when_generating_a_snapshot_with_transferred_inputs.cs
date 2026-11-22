// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_generating_a_snapshot_with_transferred_inputs : given.input_members
{
    GeneratedScreenplayDefinition _forward;
    GeneratedScreenplayDefinition _reverse;

    void Because()
    {
        var facts = InputFacts();
        var generator = new ScreenplayDefinitionGenerator();
        var options = new ScreenplayGenerationOptions { Domain = "Application" };
        _forward = generator.Generate(Run(facts), options);
        _reverse = generator.Generate(Run([.. facts.Reverse()]), options);
    }

    [Fact] void should_materialize_the_query_input() => _forward.Graph.Artifacts.Single(artifact => artifact.Key == Transfer().Definition.Artifact).Variants.Single().Definition.Properties.Single().Name.ShouldEqual("filter");
    [Fact] void should_print_the_input() => _forward.Source.ShouldContain("filter string");
    [Fact] void should_verify_the_printed_screenplay() => _forward.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_neutral_members_as_provenance() => _forward.AdapterRun.Facts.Single(record => record.Fact.Id == Surface().Id).Disposition.ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_be_independent_of_fact_enumeration() => Projection(_forward.AdapterRun).ShouldEqual(Projection(_reverse.AdapterRun));
    [Fact] void should_print_the_same_source() => _forward.Source.ShouldEqual(_reverse.Source);
}
