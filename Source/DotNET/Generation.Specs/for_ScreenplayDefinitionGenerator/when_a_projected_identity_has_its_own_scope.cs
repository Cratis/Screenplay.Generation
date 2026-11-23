// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_projected_identity_has_its_own_scope : given.compatibility_type_uses
{
    static readonly BackendScopeKey _selectedScope = new() { Store = new() { Value = "dotnet://Ordering/SelectedStore" } };
    static readonly BackendScopeKey _terminalScope = new() { Store = new() { Value = "dotnet://Ordering/TerminalStore" } };
    GeneratedScreenplayDefinition _bound = null!;
    GeneratedScreenplayDefinition _omitted = null!;

    void Because()
    {
        var original = ProjectedCommand(Target, "Container", true);
        var observed = original.OfType<ArtifactMemberTypeUseFact>().Single();
        var view = original.OfType<TypeUseCompatibilityProjectionFact>().Single();
        var exact = observed.Definition.Type;
        var nodes = exact.Nodes!;
        var inputs = original.Select(fact => fact switch
        {
            ArtifactMemberTypeUseFact typeUse => typeUse with { Definition = typeUse.Definition with { Type = exact with { ObservedScope = _terminalScope, Nodes = [nodes[0], nodes[1] with { Scope = _selectedScope }, nodes[2] with { Scope = _terminalScope }] } } },
            TypeUseCompatibilityProjectionFact projection => projection with { Definition = view.Definition with { Type = view.Definition.Type with { ObservedScope = _selectedScope } } },
            _ => fact
        }).ToArray();
        GenerationFact[] evidence =
        [
            new BackendScopeFact { Id = Id("selected-scope"), Subject = _selectedScope.Store, Evidence = observed.Evidence, Definition = new() { Key = _selectedScope } },
            new BackendScopeFact { Id = Id("terminal-scope"), Subject = _terminalScope.Store, Evidence = observed.Evidence, Definition = new() { Key = _terminalScope } },
            Facts().OfType<TypeUseTargetOmittedFact>().Single()
        ];
        var global = GlobalConcept();
        _omitted = Generate([.. inputs, .. evidence, .. global]);
        var scopedTarget = new ArtifactKey { Subject = Target, Kind = ArtifactKind.ReadModel, Scope = _selectedScope };
        var placement = original.OfType<ArtifactPlacementFact>().Single();
        _bound = Generate([.. inputs.Where(fact => fact is not TypeUseBindingPolicyFact), .. evidence, .. global,
            new ArtifactDeclarationFact { Id = Id("scoped-target"), Subject = Target, Evidence = observed.Evidence, Definition = new() { Artifact = scopedTarget, Name = "ScopedRecord" } },
            placement with { Id = Id("scoped-target-placement"), Subject = Target, Artifact = scopedTarget, Placement = placement.Placement with { Slice = "ScopedRecord", SliceKind = GenerationSliceKind.StateView } }]);
    }

    [Fact] void should_bind_the_selected_scope() => Member(_bound).Type.Scope.ShouldEqual(_selectedScope);
    [Fact] void should_not_bind_the_global_concept_with_the_same_subject() => Member(_bound).Type.TargetArtifactKind.ShouldEqual(ArtifactKind.ReadModel);
    [Fact] void should_keep_the_terminal_scope_as_exact_evidence() => _bound.AdapterRun!.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.ObservedScope.ShouldEqual(_terminalScope);
    [Fact] void should_lower_the_selected_target() => _bound.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_report_an_omission_when_the_selected_target_exists() => _bound.Diagnostics.Any(diagnostic => diagnostic.Code == "example.FlagsEnumOmitted").ShouldBeFalse();
    [Fact] void should_keep_projected_fallback_when_the_selected_concept_is_omitted() => _omitted.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_use_the_hidden_terminal_or_global_concept_as_fallback() => _omitted.Source.ShouldContain("permissions Container[]");
    [Fact] void should_report_the_owned_omission_once() => _omitted.Diagnostics.Count(diagnostic => diagnostic.Code == "example.FlagsEnumOmitted").ShouldEqual(1);

    GenerationFact[] GlobalConcept() => [.. Candidate("enum", false).Select(fact => fact switch
    {
        ArtifactDeclarationFact declaration => declaration with { Id = Id("global-declaration"), Definition = declaration.Definition with { Name = "GlobalPermissions" } },
        ConceptRepresentationFact representation => representation with { Id = Id("global-representation") },
        _ => fact
    })];
}
