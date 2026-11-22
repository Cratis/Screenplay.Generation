// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_transferring_a_declaration_input : given.input_members
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive(InputFacts());

    [Fact] void should_keep_the_normalized_member_name() => Members(_result).Single().Definition.Member.Name.ShouldEqual("filter");
    [Fact] void should_append_at_the_first_free_order() => Members(_result).Single().Definition.DeclarationOrder.ShouldEqual(0);
    [Fact] void should_preserve_the_original_reads_join() => _result.Facts.Select(record => record.Fact).OfType<RelationshipFact>().Single().Definition.Key.Kind.ShouldEqual(RelationshipKind.Reads);
    [Fact] void should_not_invent_an_identifier_role() => _result.Facts.Select(record => record.Fact).OfType<ArtifactMemberRoleFact>().ShouldBeEmpty();
    [Fact] void should_have_complete_base_fact_lineage() => _result.Facts.Single(record => record.Fact is ArtifactMemberDeclarationFact).Lineage.Inputs.ShouldContain(Transfer().Id);
    [Fact] void should_have_no_errors() => _result.Diagnostics.ShouldBeEmpty();
}
