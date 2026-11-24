// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_direct_inputs_shadow_input_only_execution : given.input_only_execution
{
    GenerationDerivationSnapshot _result;

    void Because()
    {
        var member = new ArtifactMemberKey { Artifact = Transfer().Definition.Artifact, Name = "filter" };
        var direct = new ArtifactMemberDeclarationFact
        {
            Id = new FactId { Value = "direct" }, Subject = member.Artifact.Subject, Evidence = UsageEvidence(),
            Definition = new ArtifactMemberDeclarationDefinition { Member = member, DeclarationOrder = 42 }
        };
        _result = Derive([.. InputOnlyFacts(Surface("limit", 1), SurfaceType("limit"), direct).Where(fact => fact is not MethodDeclarationExecutionFact), InputExecution(null)]);
    }

    [Fact] void should_not_duplicate_the_direct_input() => Members(_result).Select(member => member.Definition.Member.Name).ShouldEqual<string>(["limit"]);
    [Fact] void should_append_after_the_sparse_direct_position() => Members(_result).Single().Definition.DeclarationOrder.ShouldEqual(43);
    [Fact] void should_allow_one_execution_with_unknown_position() => _result.Diagnostics.ShouldBeEmpty();
}
