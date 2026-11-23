// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_a_direct_member_shadows_an_omitted_input_member : given.omitted_input_members
{
    GeneratedScreenplayDefinition _result;

    void Because()
    {
        var member = new ArtifactMemberKey { Artifact = Transfer().Definition.Artifact, Name = "filter" };
        var declaration = new ArtifactMemberDeclarationFact
        {
            Id = new FactId { Value = "direct-filter" },
            Subject = member.Artifact.Subject,
            Evidence = UsageEvidence(),
            Definition = new ArtifactMemberDeclarationDefinition { Member = member, DeclarationOrder = 0 }
        };
        var typeUse = new ArtifactMemberTypeUseFact
        {
            Id = new FactId { Value = "direct-filter-type" },
            Subject = member.Artifact.Subject,
            Evidence = UsageEvidence(),
            Definition = new ArtifactMemberTypeUseDefinition { Member = member, Type = Scalar() with { Name = "String" } }
        };
        _result = Generate([.. OmittedFacts(), declaration, typeUse]);
    }

    [Fact] void should_generate_compiler_verified_screenplay() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_direct_member_type() => _result.Source.ShouldContain("filter String");
    [Fact] void should_not_transfer_a_duplicate_member() => _result.AdapterRun.Derivation.Facts.Select(record => record.Fact).OfType<ArtifactMemberDeclarationFact>().ShouldBeEmpty();
    [Fact] void should_not_transfer_the_shadowed_omission() => _result.AdapterRun.Derivation.Facts.Select(record => record.Fact).OfType<TypeUseTargetOmittedFact>().ShouldBeEmpty();
    [Fact] void should_not_report_the_shadowed_omission_warning() => _result.Diagnostics.ShouldBeEmpty();
}
