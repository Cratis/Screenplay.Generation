// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_a_type_use_asserts_a_different_subject : given.a_derivation
{
    GenerationDerivationSnapshot _result = null!;

    void Because()
    {
        var typeUse = TypeUse("customerCode", ConceptSubject, "type-use") with
        {
            Subject = new SubjectId { Value = "dotnet://Foreign/Commands.RegisterCustomer" }
        };
        _result = Derive(
            CommandDeclaration(),
            MemberDeclaration("customerCode", 0, "member"),
            typeUse,
            Concept(ConceptSubject, "customer-code"));
    }

    [Fact] void should_not_derive_from_the_foreign_type_use() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_report_invalid_granular_ownership() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.InvalidGranularFactOwnership);
    [Fact] void should_exclude_the_foreign_fact_from_rule_inputs() => _result.Rules.Single().Inputs.Select(input => input.Value).ShouldNotContain("application:type-use");
}
