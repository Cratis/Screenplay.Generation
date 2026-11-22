// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeUseFacts;

public class when_selecting_inherited_member_origins : given.a_mixed_origin_surface
{
    IReadOnlyList<GenerationFact> _authored;
    IReadOnlyList<GenerationFact> _all;
    ArtifactMemberDeclarationFact[] _members;
    IReadOnlyList<PropertyDefinition> _properties;

    void Because()
    {
        _authored = FactsFor(DotNetMemberOriginPolicy.AuthoredOnly);
        _all = FactsFor(DotNetMemberOriginPolicy.AllOrigins);
        _members = [.. _all.OfType<ArtifactMemberDeclarationFact>()];
        _properties = PropertiesFor(DotNetMemberOriginPolicy.AllOrigins);
    }

    [Fact] void should_keep_authored_only_as_the_default() => _authored.Select(Serialize).ShouldEqual(DotNetTypeUseFacts.EmitWithSurface(Type, Artifact, Context, Evidence, DotNetMemberSurfaceScope.Inherited).Select(Serialize));
    [Fact] void should_keep_the_default_role_literal_unambiguous() => _authored.Select(Serialize).ShouldEqual(DotNetTypeUseFacts.EmitWithSurface(Type, Artifact, Context, Evidence, DotNetMemberSurfaceScope.Inherited, default).Select(Serialize));
    [Fact] void should_exclude_all_non_authored_bases_by_default() => _authored.OfType<ArtifactMemberDeclarationFact>().Select(_ => _.Definition.Member.Name).ShouldEqual("local", "value", "hidden");
    [Fact] void should_order_all_origins_derived_first_and_metadata_by_name() => _members.Select(_ => _.Definition.Member.Name).ShouldEqual("local", "value", "hidden", "other", "generatedProperty", "alpha", "externalCode", "id", "readable", "zed");
    [Fact] void should_use_the_same_surface_for_property_definitions() => _properties.Select(_ => _.Name).ShouldEqual(_members.Select(_ => _.Definition.Member.Name));
    [Fact] void should_keep_contiguous_member_order() => _members.Select(_ => _.Definition.DeclarationOrder).ShouldEqual(Enumerable.Range(0, _members.Length));
    [Fact] void should_keep_the_derived_hidden_member() => Use("hidden").Name.ShouldEqual("Int");
    [Fact] void should_keep_the_covariant_override() => Use("value").Name.ShouldEqual("CustomerCode");
    [Fact] void should_not_let_a_private_getter_hide_a_readable_base_property() => Use("readable").Name.ShouldEqual("String");
    [Fact] void should_substitute_a_metadata_generic_base_argument() => Use("id").ObservedTypeSubject.ShouldEqual(Context.SubjectForType(TypeNamed(Context.Projects[0].Compilation, "Contracts.CustomerCode")));
    [Fact] void should_apply_the_role_callback_to_the_metadata_identity() => _all.OfType<ArtifactMemberRoleFact>().Single().Definition.Role.ShouldEqual(ArtifactMemberRoleKind.Identifier);
    [Fact] void should_not_invent_a_subject_for_a_metadata_type() => Use("externalCode").ObservedTypeSubject.ShouldBeNull();
    [Fact] void should_preserve_caller_evidence_for_every_origin() => _all.All(_ => _.Evidence == Evidence).ShouldBeTrue();
    [Fact] void should_keep_the_nominated_artifact_as_owner() => _all.All(_ => _.Subject == Artifact.Subject).ShouldBeTrue();
    [Fact] void should_preserve_existing_member_ids() => _members.Take(3).Select(_ => _.Id).ShouldEqual(_authored.OfType<ArtifactMemberDeclarationFact>().Select(_ => _.Id));
    [Fact] void should_exclude_record_equality_contracts() => _members.Any(_ => _.Definition.Member.Name == "equalityContract").ShouldBeFalse();
    [Fact] void should_exclude_object_members() => DotNetTypeShapes.PropertiesOf(TypeNamed(Context.Projects[0].Compilation, "System.Object"), Context, DotNetMemberOriginPolicy.AllOrigins, DotNetMemberSurfaceScope.Inherited).ShouldBeEmpty();
    [Fact] void should_not_change_authored_tree_authority() => Context.Projects[0].AuthoredSyntaxTrees.Select(_ => _.FilePath).ShouldEqual("Authored.cs");
    [Fact] void should_not_make_a_generated_base_authoritative() => DotNetSource.HasAuthoredDeclaration(Type.BaseType!.BaseType!, Context.Projects[0].AuthoredSyntaxTrees).ShouldBeFalse();
    [Fact] void should_not_make_a_non_authored_base_authoritative() => DotNetSource.HasAuthoredDeclaration(Type.BaseType!, Context.Projects[0].AuthoredSyntaxTrees).ShouldBeFalse();
    [Fact] void should_not_make_a_metadata_base_authoritative() => DotNetSource.HasAuthoredDeclaration(Type.BaseType!.BaseType!.BaseType!, Context.Projects[0].AuthoredSyntaxTrees).ShouldBeFalse();

    static string Serialize(GenerationFact fact) => System.Text.Json.JsonSerializer.Serialize(fact, fact.GetType());

    TypeUseDefinition Use(string name) => _all.OfType<ArtifactMemberTypeUseFact>().Single(_ => _.Definition.Member.Name == name).Definition.Type;
}
