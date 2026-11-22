// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_preserving_a_wrapped_named_input_type : given.input_members
{
    GenerationDerivationSnapshot _result;

    void Because()
    {
        var type = new TypeUseDefinition { Name = "Document", Shape = [TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named], ObservedTypeSubject = Document.Subject };
        _result = Derive([.. InputFacts().Where(fact => fact is not DeclarationInputMemberTypeUseFact), SurfaceType(type: type)]);
    }

    [Fact] void should_preserve_wrapper_order() => _result.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.Shape.ShouldEqual(new[] { TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named });
    [Fact] void should_bind_the_exact_named_target_without_rule_chaining() => _result.Facts.Select(record => record.Fact).OfType<TypeUseBindingFact>().Single().Definition.Target.Subject.ShouldEqual(Document.Subject);
}
