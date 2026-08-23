// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_an_unknown_slice_kind : for_GenerationResolver.given.facts
{
    ScreenplayLoweringResult _result = null!;
    string _source = null!;

    void Because()
    {
        var artifact = EventDefinition();
        var evidence = Fact("event", FirstAdapter).Evidence;
        var graph = new ResolvedApplicationGraph
        {
            Artifacts =
            [
                new ResolvedArtifact
                {
                    Key = artifact.Key,
                    Variants =
                    [
                        new ResolvedArtifactVariant
                        {
                            Definition = artifact,
                            Evidence = [evidence]
                        }
                    ]
                }
            ],
            Placements =
            [
                new ResolvedArtifactPlacement
                {
                    Artifact = artifact.Key,
                    Variants =
                    [
                        new ResolvedArtifactPlacementVariant
                        {
                            Placement = new ArtifactPlacement
                            {
                                Module = "Accounts",
                                Features = ["Opening"],
                                Slice = "Open",
                                SliceKind = GenerationSliceKind.Unknown
                            },
                            Evidence = [evidence]
                        }
                    ]
                }
            ]
        };

        _result = new ScreenplayLowerer().Lower(graph, "Banking");
        _source = new ScreenplayPrinter().Print(_result.Application);
    }

    [Fact] void should_report_the_unknown_slice_kind() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.UnsupportedSliceKind);
    [Fact] void should_type_the_unknown_outcome() => _result.Diagnostics.Single().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Unknown);
    [Fact] void should_report_the_artifact_subject() => _result.Diagnostics.Single().Subject.ShouldEqual(EventSubject);
    [Fact] void should_preserve_the_placement_source() => _result.Diagnostics.Single().Source!.Path.ShouldEqual("Accounts/Open/AccountOpened.cs");
    [Fact] void should_not_fabricate_state_change_semantics() => _source.ShouldNotContain("state change");
    [Fact] void should_not_emit_the_event_in_another_semantic_role() => _source.ShouldNotContain("event AccountOpened");
}
