// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_subjectless_aggregate_property_has_a_bound_type_use : given.yielding_concepts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var facts = Command().Select(fact => fact is ArtifactMemberTypeUseFact typeUse
            ? typeUse with { Definition = typeUse.Definition with { Type = typeUse.Definition.Type with { Name = "Permissions" } } }
            : fact).ToArray();
        var declaration = facts.OfType<ArtifactDeclarationFact>().Single();
        var member = facts.OfType<ArtifactMemberTypeUseFact>().Single().Definition.Member;
        _result = Generate(
        [
            .. facts,
            .. Candidate("enum", true),
            new ArtifactFact
            {
                Id = Id("aggregate-command"),
                Subject = declaration.Subject,
                Evidence = declaration.Evidence,
                Definition = new ArtifactDefinition
                {
                    Key = declaration.Definition.Artifact,
                    Name = declaration.Definition.Name,
                    Properties = [new PropertyDefinition { Name = member.Name, Type = new TypeReferenceDefinition { Name = "Permissions" } }]
                }
            }
        ]);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_yielding_enum() => _result.Source.ShouldContain("concept Permissions : Enum");
    [Fact] void should_keep_the_members_concept_binding() => Member(_result).Type.Subject.ShouldEqual(Target);
    [Fact] void should_not_reserve_the_bound_propertys_name() => _result.Diagnostics.Any(IsWarning).ShouldBeFalse();
}
