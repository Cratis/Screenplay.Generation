// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_rejecting_malformed_projection_views : given.compatibility_type_uses
{
    IReadOnlyDictionary<string, GeneratedScreenplayDefinition> _results = null!;

    void Because()
    {
        var facts = ProjectedCommand();
        var view = facts.OfType<TypeUseCompatibilityProjectionFact>().Single();
        TypeUseCompatibilityProjectionFact[] invalid =
        [
            view with { Definition = view.Definition with { Version = 2 } },
            view with { Definition = view.Definition with { Profile = TypeUseCompatibilityProfile.Unknown } },
            view with { Definition = view.Definition with { Profile = (TypeUseCompatibilityProfile)999 } },
            view with { Definition = view.Definition with { TypeUse = Id("missing-observation") } },
            view with { Subject = Target },
            view with { Evidence = view.Evidence with { Adapter = new() { Id = "foreign", Version = "1" } } },
            view with { Evidence = view.Evidence with { Source = view.Evidence.Source! with { StartColumn = 2 } } },
            view with { Definition = view.Definition with { SourceNodeIndex = 2 } },
            view with { Definition = view.Definition with { Type = view.Definition.Type with { Name = "Other" } } }
        ];
        _results = invalid.Select((projection, index) => (index, result: Generate(facts.Where(fact => fact is not TypeUseCompatibilityProjectionFact).Append(projection))))
            .ToDictionary(pair => pair.index.ToString(System.Globalization.CultureInfo.InvariantCulture), pair => pair.result);
    }

    [Fact] void should_reject_future_versions() => _results["0"].IsSuccess.ShouldBeFalse();
    [Fact] void should_reject_unknown_profiles() => _results["1"].IsSuccess.ShouldBeFalse();
    [Fact] void should_reject_undefined_profiles() => _results["2"].IsSuccess.ShouldBeFalse();
    [Fact] void should_reject_missing_observations() => _results["3"].IsSuccess.ShouldBeFalse();
    [Fact] void should_reject_foreign_owners() => _results["4"].IsSuccess.ShouldBeFalse();
    [Fact] void should_reject_foreign_adapters() => _results["5"].IsSuccess.ShouldBeFalse();
    [Fact] void should_reject_foreign_sources() => _results["6"].IsSuccess.ShouldBeFalse();
    [Fact] void should_reject_the_wrong_node() => _results["7"].IsSuccess.ShouldBeFalse();
    [Fact] void should_reject_the_wrong_identity() => _results["8"].IsSuccess.ShouldBeFalse();
    [Fact] void should_report_every_rejection() => _results.Values.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Severity == GenerationDiagnosticSeverity.Error)).ShouldBeTrue();
}
