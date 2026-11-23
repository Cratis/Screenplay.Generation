// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_scoped_owner_uses_a_yielding_global_concept : given.yielding_concepts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var facts = Command();
        var scope = new BackendScopeKey { Store = new SubjectId { Value = "dotnet://Ordering/Store" } };
        var scoped = facts.Select(fact => WithScope(fact, scope));
        _result = Generate(
        [
            .. scoped,
            .. Candidate("enum", true),
            .. Candidate("vogen", false, true),
            new BackendScopeFact
            {
                Id = Id("scope"), Subject = scope.Store, Evidence = facts[0].Evidence,
                Definition = new BackendScopeDefinition { Key = scope, Name = "Ordering" }
            }
        ]);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_scoped_command_with_an_unbound_type() => _result.Source.ShouldContain("permissions Int");
    [Fact] void should_not_bind_to_the_same_named_concept() => Member(_result).Type.Subject.ShouldBeNull();
    [Fact] void should_not_report_a_missing_global_target() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).ShouldBeFalse();
    [Fact] void should_preserve_the_global_observed_scope() => _result.AdapterRun!.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.ObservedScope.ShouldBeNull();

    static GenerationFact WithScope(GenerationFact fact, BackendScopeKey scope) => fact switch
    {
        ArtifactDeclarationFact declaration => declaration with { Definition = declaration.Definition with { Artifact = declaration.Definition.Artifact with { Scope = scope } } },
        ArtifactPlacementFact placement => placement with { Artifact = placement.Artifact with { Scope = scope } },
        ArtifactMemberDeclarationFact member => member with { Definition = member.Definition with { Member = member.Definition.Member with { Artifact = member.Definition.Member.Artifact with { Scope = scope } } } },
        ArtifactMemberTypeUseFact typeUse => typeUse with { Definition = typeUse.Definition with { Member = typeUse.Definition.Member with { Artifact = typeUse.Definition.Member.Artifact with { Scope = scope } } } },
        _ => fact
    };
}
