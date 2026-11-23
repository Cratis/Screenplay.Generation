// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_supplemental_nodes_are_optional : given.compatibility_type_uses
{
    GeneratedScreenplayDefinition _consistent = null!;
    GeneratedScreenplayDefinition _conflicting = null!;

    void Because()
    {
        var facts = Command(TypeUseShapeKind.Collection, TypeUseShapeKind.Named);
        var original = facts.OfType<ArtifactMemberTypeUseFact>().Single();
        var supplied = original with
        {
            Id = Id("supplemental-observation"),
            Definition = original.Definition with { Type = original.Definition.Type with { Nodes = [new() { Name = "List" }, new() { Name = original.Definition.Type.Name, Subject = Target }] } }
        };
        _consistent = Generate([.. facts, supplied, .. Candidate("enum", false)]);
        var incompatible = supplied with
        {
            Id = Id("conflicting-supplemental-observation"),
            Definition = supplied.Definition with { Type = supplied.Definition.Type with { Nodes = [new() { Name = "DifferentContainer" }, supplied.Definition.Type.Nodes[1]] } }
        };
        _conflicting = Generate([.. facts, supplied, incompatible, .. Candidate("enum", false)]);
    }

    [Fact] void should_not_treat_missing_metadata_as_a_type_conflict() => _consistent.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_existing_printing() => _consistent.Source.ShouldContain("permissions Permissions[]");
    [Fact] void should_keep_both_observations() => _consistent.AdapterRun!.Facts.Count(record => record.Fact is ArtifactMemberTypeUseFact).ShouldEqual(2);
    [Fact] void should_reject_conflicting_supplied_metadata() => _conflicting.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingMemberTypeUse).ShouldBeTrue();
}
