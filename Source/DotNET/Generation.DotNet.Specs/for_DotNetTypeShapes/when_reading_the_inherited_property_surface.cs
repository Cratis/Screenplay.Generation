// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeShapes;

public class when_reading_the_inherited_property_surface : given.an_inherited_surface
{
    IReadOnlyList<PropertyDefinition> _properties;
    IReadOnlyList<GenerationFact> _facts;

    void Because()
    {
        _properties = DotNetTypeShapes.PropertiesOf(Type, Context, DotNetMemberSurfaceScope.Inherited);
        _facts = DotNetTypeUseFacts.EmitWithSurface(Type, Artifact, Context, Evidence, DotNetMemberSurfaceScope.Inherited);
    }

    [Fact] void should_pair_legacy_and_granular_members_in_the_same_order() => _properties.Select(property => property.Name).ShouldEqual(_facts.OfType<ArtifactMemberDeclarationFact>().Select(fact => fact.Definition.Member.Name));
    [Fact] void should_retain_the_substituted_terminal_subject() => _properties.Single(property => property.Name == "codes").Type.Subject.ShouldEqual(CodeSubject);
    [Fact] void should_retain_collection_shape() => _properties.Single(property => property.Name == "codes").Type.IsCollection.ShouldBeTrue();
    [Fact] void should_leave_old_properties_overloads_declared_only() => DotNetTypeShapes.PropertiesOf(Type, Context).Count.ShouldEqual(4);
    [Fact] void should_leave_old_emit_default_literal_calls_unambiguous_and_declared_only() => DotNetTypeUseFacts.Emit(Type, Artifact, Context, Evidence, default).OfType<ArtifactMemberDeclarationFact>().Count().ShouldEqual(4);
    [Fact] void should_reject_undefined_scopes() => Catch.Exception(() => DotNetTypeShapes.PropertiesOf(Type, Context, (DotNetMemberSurfaceScope)42)).ShouldBeOfExactType<ArgumentOutOfRangeException>();
}
