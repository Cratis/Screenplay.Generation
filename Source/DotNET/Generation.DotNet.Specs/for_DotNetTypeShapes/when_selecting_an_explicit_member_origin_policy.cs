// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeShapes;

public class when_selecting_an_explicit_member_origin_policy : given.an_inherited_surface
{
    IReadOnlyList<PropertyDefinition> _authored;
    IReadOnlyList<PropertyDefinition> _all;

    void Because()
    {
        _authored = DotNetTypeShapes.PropertiesOf(Type, Context, DotNetMemberOriginPolicy.AuthoredOnly, DotNetMemberSurfaceScope.Inherited);
        _all = DotNetTypeShapes.PropertiesOf(Type, Context, DotNetMemberOriginPolicy.AllOrigins, DotNetMemberSurfaceScope.Inherited);
    }

    [Fact] void should_keep_the_implicit_authored_surface() => _authored.ShouldEqual(DotNetTypeShapes.PropertiesOf(Type, Context, DotNetMemberSurfaceScope.Inherited));
    [Fact] void should_keep_the_same_authored_surface_under_both_policies() => _all.ShouldEqual(_authored);
    [Fact] void should_preserve_authored_generic_substitution() => _all.Single(_ => _.Name == "codes").Type.Subject.ShouldEqual(CodeSubject);
    [Fact] void should_preserve_shadowing_and_order() => _all.Select(_ => _.Name).ShouldEqual(["partial", "local", "value", "hidden", "codes", "readable"]);
    [Fact] void should_not_change_declared_mode() => DotNetTypeShapes.PropertiesOf(Type, Context, DotNetMemberOriginPolicy.AllOrigins, DotNetMemberSurfaceScope.Declared).ShouldEqual(DotNetTypeShapes.PropertiesOf(Type, Context));
    [Fact] void should_not_change_declared_fact_emission() => DotNetTypeUseFacts.EmitWithSurface(Type, Artifact, Context, Evidence, DotNetMemberOriginPolicy.AllOrigins, DotNetMemberSurfaceScope.Declared).Select(Serialize).ShouldEqual(DotNetTypeUseFacts.Emit(Type, Artifact, Context, Evidence).Select(Serialize));
    [Fact] void should_preserve_authored_facts_under_both_policies() => DotNetTypeUseFacts.EmitWithSurface(Type, Artifact, Context, Evidence, DotNetMemberOriginPolicy.AllOrigins, DotNetMemberSurfaceScope.Inherited).Select(Serialize).ShouldEqual(DotNetTypeUseFacts.EmitWithSurface(Type, Artifact, Context, Evidence, DotNetMemberSurfaceScope.Inherited).Select(Serialize));
    [Fact] void should_reject_undefined_origin_policies() => Catch.Exception(() => DotNetTypeShapes.PropertiesOf(Type, Context, (DotNetMemberOriginPolicy)42, DotNetMemberSurfaceScope.Inherited)).ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_reject_undefined_origin_policies_for_declared_mode() => Catch.Exception(() => DotNetTypeShapes.PropertiesOf(Type, Context, (DotNetMemberOriginPolicy)42, DotNetMemberSurfaceScope.Declared)).ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_reject_undefined_scopes_under_the_all_origins_policy() => Catch.Exception(() => DotNetTypeShapes.PropertiesOf(Type, Context, DotNetMemberOriginPolicy.AllOrigins, (DotNetMemberSurfaceScope)42)).ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_reject_undefined_origin_policies_when_emitting_facts() => Catch.Exception(() => DotNetTypeUseFacts.EmitWithSurface(Type, Artifact, Context, Evidence, (DotNetMemberOriginPolicy)42, DotNetMemberSurfaceScope.Inherited)).ShouldBeOfExactType<ArgumentOutOfRangeException>();

    static string Serialize(GenerationFact fact) => System.Text.Json.JsonSerializer.Serialize(fact, fact.GetType());
}
