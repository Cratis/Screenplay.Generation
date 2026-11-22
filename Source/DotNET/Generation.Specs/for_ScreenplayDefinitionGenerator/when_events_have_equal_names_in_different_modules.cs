// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_events_have_equal_names_in_different_modules : for_GenerationResolver.given.backend_scopes
{
    GeneratedScreenplayDefinition _result;
    GeneratedScreenplayDefinition _differentSlices;
    GeneratedScreenplayDefinition _differentFeatures;

    void Because()
    {
        var first = Key(EventSubject, ArtifactKind.Event, null);
        var second = first with { Subject = new SubjectId { Value = "dotnet://Other/Events.CustomerRegistered" } };
        var firstPlacement = Placement(first, "first-placement");
        var secondPlacement = Placement(second, "second-placement");
        _result = Generate(
            Artifact(first, "first", "CustomerRegistered"),
            Artifact(second, "second", "CustomerRegistered"),
            firstPlacement with { Placement = firstPlacement.Placement with { Module = "First" } },
            secondPlacement with { Placement = secondPlacement.Placement with { Module = "Second" } });
        _differentSlices = Generate(
            Artifact(first, "first", "CustomerRegistered"),
            Artifact(second, "second", "CustomerRegistered"),
            firstPlacement with { Placement = firstPlacement.Placement with { Slice = "First" } },
            secondPlacement with { Placement = secondPlacement.Placement with { Slice = "Second" } });
        _differentFeatures = Generate(
            Artifact(first, "first", "CustomerRegistered"),
            Artifact(second, "second", "CustomerRegistered"),
            firstPlacement with { Placement = firstPlacement.Placement with { Features = ["First"] } },
            secondPlacement with { Placement = secondPlacement.Placement with { Features = ["Second"] } });
    }

    [Fact] void should_respect_the_pinned_compilers_separate_module_namespaces() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_verify_both_declarations() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_respect_distinct_slice_namespaces() => _differentSlices.Diagnostics.ShouldBeEmpty();
    [Fact] void should_respect_distinct_feature_namespaces() => _differentFeatures.Diagnostics.ShouldBeEmpty();
}
