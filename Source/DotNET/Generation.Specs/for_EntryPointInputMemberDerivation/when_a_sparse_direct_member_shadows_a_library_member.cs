// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_a_sparse_direct_member_shadows_a_library_member : given.input_members
{
    GenerationDerivationSnapshot _result;

    void Because()
    {
        var member = new ArtifactMemberKey { Artifact = Transfer().Definition.Artifact, Name = "filter" };
        var declaration = new ArtifactMemberDeclarationFact { Id = new FactId { Value = "direct" }, Subject = member.Artifact.Subject, Evidence = UsageEvidence(), Definition = new ArtifactMemberDeclarationDefinition { Member = member, DeclarationOrder = 9 } };
        var type = new ArtifactMemberTypeUseFact { Id = new FactId { Value = "direct-type" }, Subject = member.Artifact.Subject, Evidence = UsageEvidence(), Definition = new ArtifactMemberTypeUseDefinition { Member = member, Type = Scalar() } };
        _result = Derive(InputFacts(declaration, type, Surface("limit", 1), SurfaceType("limit")));
    }

    [Fact] void should_never_replace_the_direct_member() => Members(_result).Any(fact => fact.Definition.Member.Name == "filter").ShouldBeFalse();
    [Fact] void should_append_after_the_actual_maximum_order() => Members(_result).Single().Definition.DeclarationOrder.ShouldEqual(10);
}
