// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_independently_proved_reads : for_GenerationFactDerivation.given.an_artifact_usage
{
    GeneratedScreenplayDefinition _result;
    GeneratedScreenplayDefinition _reversed;

    void Because()
    {
        var facts = CompleteFacts();
        AdapterContribution[] contributions =
        [
            new() { Adapter = ConceptAdapter, Facts = [.. facts.Where(fact => fact.Evidence.Adapter == ConceptAdapter), facts.Single(fact => fact is ArtifactDeclarationFact artifact && artifact.Definition.Artifact == Document), Placement(Document)] },
            new() { Adapter = ApplicationAdapter, Facts = [.. facts.Where(fact => fact.Evidence.Adapter == ApplicationAdapter && fact.Id.Value != "application:document"), Placement(Query)] }
        ];
        var generator = new ScreenplayDefinitionGenerator();
        var options = new ScreenplayGenerationOptions { Domain = "Banking" };
        _result = generator.Generate(contributions, options);
        _reversed = generator.Generate(contributions.Reverse(), options);
    }

    [Fact] void should_verify_the_joined_document() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_lower_the_read_relationship() => _result.Source.ShouldContain("reads Customer");
    [Fact] void should_ignore_adapter_order() => _reversed.Source.ShouldEqual(_result.Source);
    [Fact] void should_report_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();

    static ArtifactPlacementFact Placement(ArtifactKey artifact) => new()
    {
        Id = new FactId { Value = $"placement:{artifact.Kind}" },
        Subject = artifact.Subject,
        Artifact = artifact,
        Placement = new ArtifactPlacement { Module = "Customers", Features = ["Lookup"], Slice = artifact.Kind == ArtifactKind.Command ? "Lookup" : "View", SliceKind = artifact.Kind == ArtifactKind.Command ? GenerationSliceKind.StateChange : GenerationSliceKind.StateView },
        Evidence = UsageEvidence()
    };
}
