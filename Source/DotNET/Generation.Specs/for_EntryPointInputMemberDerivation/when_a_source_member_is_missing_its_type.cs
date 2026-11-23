// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_a_source_member_is_missing_its_type : given.input_members
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. InputFacts().Where(fact => fact is not DeclarationInputMemberTypeUseFact)]);

    [Fact] void should_not_invent_a_string_type() => Members(_result).ShouldBeEmpty();
    [Fact] void should_emit_a_located_error() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidEntryPointInputTransfer).Severity.ShouldEqual(GenerationDiagnosticSeverity.Error);
    [Fact] void should_locate_the_real_source() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidEntryPointInputTransfer).Source.ShouldNotBeNull();
}
