// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeUseFacts;

public class when_emitting_the_inherited_property_surface : given.an_inherited_surface
{
    IReadOnlyList<GenerationFact> _facts;

    void Because() => _facts = DotNetTypeUseFacts.EmitWithSurface(Type, Artifact, Context, Evidence, DotNetMemberSurfaceScope.Inherited, property => property.Name == "Codes" ? ArtifactMemberRoleKind.Identifier : null);

    [Fact] void should_keep_derived_first_order_and_only_readable_members() => _facts.OfType<ArtifactMemberDeclarationFact>().Select(fact => fact.Definition.Member.Name).ShouldEqual("partial", "local", "value", "hidden", "codes", "readable");
    [Fact] void should_assign_contiguous_ordinals() => _facts.OfType<ArtifactMemberDeclarationFact>().Select(fact => fact.Definition.DeclarationOrder).ShouldEqual(0, 1, 2, 3, 4, 5);
    [Fact] void should_retain_substituted_generic_terminal_identity() => Use("codes").ObservedTypeSubject.ShouldEqual(CodeSubject);
    [Fact] void should_retain_nested_wrappers() => Use("codes").Shape.ShouldEqual(TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named);
    [Fact] void should_retain_the_covariant_override() => Use("value").ObservedTypeSubject.ShouldEqual(CodeSubject);
    [Fact] void should_retain_new_hiding() => Use("hidden").Name.ShouldEqual("Int");
    [Fact] void should_not_let_a_private_getter_hide_a_readable_base() => Use("readable").Name.ShouldEqual("String");
    [Fact] void should_emit_unique_fact_ids() => _facts.Select(fact => fact.Id).Distinct().Count().ShouldEqual(_facts.Count);
    [Fact] void should_retain_caller_evidence_and_derived_ownership() => _facts.All(fact => fact.Evidence == Evidence && fact.Subject == Artifact.Subject).ShouldBeTrue();
    [Fact] void should_apply_roles_only_to_selected_members() => _facts.OfType<ArtifactMemberRoleFact>().Single().Definition.Member.Name.ShouldEqual("codes");

    TypeUseDefinition Use(string name) => _facts.OfType<ArtifactMemberTypeUseFact>().Single(fact => fact.Definition.Member.Name == name).Definition.Type;
}
