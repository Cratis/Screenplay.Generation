// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_raw_projection_payloads_are_malformed : given.compatibility_type_uses
{
    GeneratedScreenplayDefinition[] _malformed = null!;
    GeneratedScreenplayDefinition _duplicate = null!;
    GeneratedScreenplayDefinition _nodes = null!;
    TypeUseCompatibilityProjectionFact _view = null!;

    void Because()
    {
        var facts = ProjectedCommand();
        _view = facts.OfType<TypeUseCompatibilityProjectionFact>().Single();
        TypeUseCompatibilityProjectionFact[] invalid =
        [
            _view with { Definition = null! },
            _view with { Definition = _view.Definition with { TypeUse = null! } },
            _view with { Definition = _view.Definition with { Type = null! } },
            _view with { Definition = _view.Definition with { Type = _view.Definition.Type with { Shape = null! } } },
            _view with { Definition = _view.Definition with { Type = _view.Definition.Type with { Shape = [] } } },
            _view with { Definition = _view.Definition with { Type = _view.Definition.Type with { Name = "" } } }
        ];
        _malformed = [.. invalid.Select(view => Generate(facts.Where(fact => fact is not TypeUseCompatibilityProjectionFact).Append(view)))];
        _duplicate = Generate([.. facts, _view with { Id = Id("duplicate-view") }]);
        var original = facts.OfType<ArtifactMemberTypeUseFact>().Single();
        _nodes = Generate([.. facts.Where(fact => fact is not ArtifactMemberTypeUseFact), original with
        {
            Definition = original.Definition with { Type = original.Definition.Type with { Nodes = [null!] } }
        }]);
    }

    [Fact] void should_reject_every_malformed_payload_without_throwing() => _malformed.All(result => !result.IsSuccess).ShouldBeTrue();
    [Fact] void should_locate_every_rejected_view() => _malformed.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidTypeUseCompatibilityProjection && diagnostic.Source == _view.Evidence.Source && diagnostic.Facts.Contains(_view.Id))).ShouldBeTrue();
    [Fact] void should_reject_duplicate_views_even_when_they_agree() => _duplicate.IsSuccess.ShouldBeFalse();
    [Fact] void should_locate_both_duplicate_views() => _duplicate.Diagnostics.Where(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidTypeUseCompatibilityProjection).Select(diagnostic => diagnostic.Facts[0]).Distinct().Count().ShouldEqual(2);
    [Fact] void should_reject_incomplete_node_evidence() => _nodes.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_invalid_node_evidence() => _nodes.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidTypeUseCompatibilityProjection).ShouldBeTrue();
}
