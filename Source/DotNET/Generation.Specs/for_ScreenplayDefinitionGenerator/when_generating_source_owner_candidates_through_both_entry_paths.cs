// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_source_owner_candidates_through_both_entry_paths : for_GenerationFactDerivation.given.source_placement_candidates
{
    GeneratedScreenplayDefinition _direct = null!;
    GeneratedScreenplayDefinition _snapshot = null!;

    void Because()
    {
        GenerationFact[] facts = [CommandDeclaration(), Candidate(Command, Command, "owner")];
        var options = new ScreenplayGenerationOptions { Domain = "Ordering", SourcePlacementResolver = Resolver() };
        var generator = new ScreenplayDefinitionGenerator();
        _direct = generator.Generate([new AdapterContribution { Adapter = ApplicationAdapter, Facts = facts }], options);
        var descriptor = new AdapterDescriptor { Identity = ApplicationAdapter, SourceLanguage = AdapterSourceLanguage.SourceIndependent, Category = AdapterCategory.ApplicationFramework };
        var run = new AdapterRunRecord
        {
            Considered = true, Probed = true, Executed = true, Descriptor = descriptor, Probe = new AdapterProbeApplicable(),
            Execution = new AdapterExecutionCompleted { Contribution = new AdapterContributionSnapshot { Descriptor = descriptor, Facts = [.. facts] } },
            Disposition = AdapterRunDisposition.Admitted
        };
        _snapshot = generator.Generate(new AdapterRunSnapshot { Adapters = [run], Facts = [.. facts.Select(fact => new GenerationFactRecord { Fact = fact })] }, options);
    }

    [Fact] void should_map_the_exact_command() => _direct.Graph.Placements.Single().Artifact.ShouldEqual(Command);
    [Fact] void should_generate_identical_placement_graphs() => Projection(_direct.Graph.Placements).ShouldEqual(Projection(_snapshot.Graph.Placements));
    [Fact] void should_generate_identical_sources() => _direct.Source.ShouldEqual(_snapshot.Source);
    [Fact] void should_compile_and_verify() => _direct.IsSuccess.ShouldBeTrue();
}
