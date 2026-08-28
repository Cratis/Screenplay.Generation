// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_member_names_contain_unpaired_utf16_code_units : given.a_derivation
{
    const string MemberName = "customer\ud800Code";
    GenerationDerivationSnapshot _result = null!;

    void Because() => _result = Derive(
        CommandDeclaration(),
        MemberDeclaration(MemberName, 0, "member"),
        TypeUse(MemberName, ConceptSubject, "type-use"),
        Concept(ConceptSubject, "customer-code"));

    [Fact] void should_derive_without_throwing() => _result.Facts.Length.ShouldEqual(1);
    [Fact] void should_reversibly_encode_every_utf16_code_unit() => _result.Facts.Single().Fact.Id.Value.ShouldContain("D800");
    [Fact] void should_retain_the_exact_member_name() => ((TypeUseBindingFact)_result.Facts.Single().Fact).Definition.Member.Name.ShouldEqual(MemberName);
}
