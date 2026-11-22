// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_reads_has_no_transfer_intent : given.input_members
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. InputFacts().Where(fact => fact is not EntryPointInputTransferFact)]);

    [Fact] void should_not_copy_any_input_members() => Members(_result).ShouldBeEmpty();
    [Fact] void should_keep_the_reads_relationship() => _result.Facts.Select(record => record.Fact).OfType<RelationshipFact>().Count().ShouldEqual(1);
    [Fact] void should_not_record_the_opt_in_rule() => _result.Rules.Any(record => record.Rule.Id == "cratis.screenplay.entry-point-input-members").ShouldBeFalse();
}
