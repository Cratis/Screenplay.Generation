// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_required_binding_inputs_are_missing_or_conflicted : given.a_derivation
{
    GenerationDerivationSnapshot _conflicted = null!;
    GenerationDerivationSnapshot _missingMember = null!;
    GenerationDerivationSnapshot _missingOwner = null!;
    GenerationDerivationSnapshot _missingTarget = null!;

    void Because()
    {
        var otherTarget = new SubjectId { Value = "dotnet://Ordering/Concepts.OtherCustomerCode" };
        _conflicted = Derive(
            CommandDeclaration(),
            MemberDeclaration("customerCode", 0, "member"),
            Concept(ConceptSubject, "customer-code"),
            Concept(otherTarget, "other-customer-code"),
            TypeUse("customerCode", ConceptSubject, "first-use"),
            TypeUse("customerCode", otherTarget, "second-use"));
        _missingOwner = Derive(TypeUse("customerCode", ConceptSubject, "missing-owner"), Concept(ConceptSubject, "target"));
        _missingMember = Derive(CommandDeclaration(), TypeUse("customerCode", ConceptSubject, "missing-member"), Concept(ConceptSubject, "target"));
        _missingTarget = Derive(CommandDeclaration(), MemberDeclaration("customerCode", 0, "member"), TypeUse("customerCode", ConceptSubject, "missing-target"));
    }

    [Fact] void should_not_choose_one_conflicting_type_use() => _conflicted.Facts.ShouldBeEmpty();
    [Fact] void should_report_the_conflicting_type_uses() => Codes(_conflicted).ShouldContain(GenerationDiagnosticCodes.ConflictingMemberTypeUse);
    [Fact] void should_fail_closed_without_an_owner() => Codes(_missingOwner).ShouldContain(GenerationDiagnosticCodes.MissingTypeUseOwner);
    [Fact] void should_fail_closed_without_a_member() => Codes(_missingMember).ShouldContain(GenerationDiagnosticCodes.MissingTypeUseMember);
    [Fact] void should_fail_closed_without_a_target() => Codes(_missingTarget).ShouldContain(GenerationDiagnosticCodes.MissingTypeUseTarget);
    [Fact] void should_identify_every_affected_input_fact_canonically() => _conflicted.Diagnostics.Single().Message.ShouldContain("'application:first-use', 'application:second-use'");

    static IEnumerable<string> Codes(GenerationDerivationSnapshot snapshot) =>
        snapshot.Diagnostics.Select(diagnostic => diagnostic.Code);
}
