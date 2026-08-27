// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_a_primitive_type_use_has_no_observed_subject : given.a_derivation
{
    GenerationDerivationSnapshot _result = null!;

    void Because() => _result = Derive(
        CommandDeclaration(),
        MemberDeclaration("customerCode", 0, "member"),
        TypeUse("customerCode", null, "primitive-use"));

    [Fact] void should_not_invent_an_artifact_binding() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_not_report_false_derivation_loss() => _result.Diagnostics.ShouldBeEmpty();
}
