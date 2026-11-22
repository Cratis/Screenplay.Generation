// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_declaration_inputs_belong_to_another_backend_scope : given.input_members
{
    GenerationDerivationSnapshot _result;

    void Because()
    {
        var scope = new BackendScopeKey { Store = new SubjectId { Value = "store:other" }, EventModelName = "other" };
        var backend = new BackendScopeFact { Id = new FactId { Value = "other-scope" }, Subject = scope.Store, Evidence = UsageEvidence(), Definition = new BackendScopeDefinition { Key = scope, Name = "Other" } };
        _result = Derive([.. InputFacts().Where(fact => fact is not (DeclarationInputMemberDeclarationFact or DeclarationInputMemberTypeUseFact)), backend,
            Surface() with { Definition = Surface().Definition with { Member = Member() with { Scope = scope } } },
            SurfaceType() with { Definition = SurfaceType().Definition with { Member = Member() with { Scope = scope } } }]);
    }

    [Fact] void should_not_copy_a_same_named_declaration_from_another_occurrence() => Members(_result).ShouldBeEmpty();
}
