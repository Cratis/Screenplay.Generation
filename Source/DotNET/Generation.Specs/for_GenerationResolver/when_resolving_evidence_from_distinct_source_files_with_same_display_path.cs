// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_evidence_from_distinct_source_files_with_same_display_path : given.facts
{
    ResolvedApplicationGraph _forward = null!;
    ResolvedApplicationGraph _reverse = null!;

    void Because()
    {
        var first = FactWithIdentity("first", "Banking\u001fCore", "Order.cs");
        var second = FactWithIdentity("second", "Banking", "Core\u001fOrder.cs");
        _forward = new GenerationResolver().Resolve([Contribution(FirstAdapter, first, second)]);
        _reverse = new GenerationResolver().Resolve([Contribution(FirstAdapter, second, first)]);
    }

    [Fact] void should_retain_both_project_qualified_pieces_of_evidence() => _forward.Artifacts.Single().Variants.Single().Evidence.Count.ShouldEqual(2);
    [Fact] void should_order_identity_bearing_evidence_independently_of_input_order() => JsonSerializer.Serialize(_reverse.Artifacts.Single().Variants.Single().Evidence).ShouldEqual(JsonSerializer.Serialize(_forward.Artifacts.Single().Variants.Single().Evidence));
    [Fact] void should_preserve_the_shared_adapter_and_display_path() => _forward.Artifacts.Single().Variants.Single().Evidence.All(_ => _.Adapter == FirstAdapter && _.Source.Path == "Common/Order.cs").ShouldBeTrue();

    static ArtifactFact FactWithIdentity(string id, string project, string path) => new()
    {
        Id = new FactId { Value = id },
        Subject = EventSubject,
        Definition = EventDefinition(),
        Evidence = new Evidence
        {
            Adapter = FirstAdapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange
            {
                Path = "Common/Order.cs",
                FileIdentity = new SourceFileIdentity { Project = project, Path = path },
                StartLine = 1,
                StartColumn = 1,
                EndLine = 1,
                EndColumn = 10
            }
        }
    };
}
