// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_input_only_execution_selects_exact_scopes : given.input_only_execution
{
    GenerationDerivationSnapshot _result;

    void Because()
    {
        var target = Scope("library");
        var facts = InputOnlyFacts().Select(fact => fact switch
        {
            MethodDeclarationExecutionFact execution => execution with { Definition = execution.Definition with { TargetScope = target } },
            DeclarationInputMemberDeclarationFact member => member with { Definition = member.Definition with { Member = member.Definition.Member with { Scope = target } } },
            DeclarationInputMemberTypeUseFact typeUse => typeUse with { Definition = typeUse.Definition with { Member = typeUse.Definition.Member with { Scope = target } } },
            _ => fact
        });
        _result = Derive([.. facts, Backend(target), SurfaceType(type: new TypeUseDefinition { Name = "int", Shape = [TypeUseShapeKind.Named] })]);
    }

    [Fact] void should_select_only_the_exact_declaration_occurrence() => _result.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.Name.ShouldEqual("string");
    [Fact] void should_transfer_into_the_global_query() => Members(_result).Single().Definition.Member.Artifact.ShouldEqual(Transfer().Definition.Artifact);
    [Fact] void should_preserve_the_exact_execution_in_lineage() => _result.Facts.Single(record => record.Fact is ArtifactMemberDeclarationFact).Lineage.Inputs.ShouldContain(InputExecution().Id);
}
