// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_same_named_concepts_have_different_exact_subjects : given.a_derivation
{
    readonly SubjectId _otherConcept = new() { Value = "dotnet://Shipping/Concepts.CustomerCode" };
    readonly SubjectId _nonDotNetConcept = new() { Value = "typescript://catalog/CustomerCode" };
    GenerationDerivationSnapshot _dotNet = null!;
    GenerationDerivationSnapshot _nonDotNet = null!;

    void Because()
    {
        var declarations = new GenerationFact[]
        {
            CommandDeclaration(),
            MemberDeclaration("customerCode", 0, "member"),
            Concept(_otherConcept, "shipping-customer-code"),
            Concept(ConceptSubject, "ordering-customer-code"),
            Concept(_nonDotNetConcept, "catalog-customer-code")
        };
        _dotNet = Derive([.. declarations, TypeUse("customerCode", ConceptSubject, "dotnet-type-use")]);
        _nonDotNet = Derive([.. declarations, TypeUse("customerCode", _nonDotNetConcept, "typescript-type-use")]);
    }

    [Fact] void should_bind_only_the_exact_dotnet_subject() => Binding(_dotNet).Definition.Target.Subject.ShouldEqual(ConceptSubject);
    [Fact] void should_allow_a_non_dotnet_frontend_subject() => Binding(_nonDotNet).Definition.Target.Subject.ShouldEqual(_nonDotNetConcept);
    [Fact] void should_not_conflict_on_equal_display_names() => _dotNet.Diagnostics.Concat(_nonDotNet.Diagnostics).ShouldBeEmpty();

    static TypeUseBindingFact Binding(GenerationDerivationSnapshot snapshot) =>
        (TypeUseBindingFact)snapshot.Facts.Single().Fact;
}
