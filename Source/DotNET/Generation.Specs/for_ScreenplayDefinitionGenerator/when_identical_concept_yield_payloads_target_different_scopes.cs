// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_identical_concept_yield_payloads_target_different_scopes : given.co_owned_yielding_concepts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because() => _result = Generate([.. InScope("first", "Sales"), .. InScope("second", "Support"), .. Candidate("vogen", false, true)]);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_merge_distinct_scoped_declarations() => _result.Diagnostics.Count(IsWarning).ShouldEqual(2);
    [Fact] void should_have_identical_diagnostic_payloads() => _result.Diagnostics.Where(IsWarning).Select(diagnostic => AdapterRunProjection(diagnostic with { Facts = [], Lineage = null })).Distinct().Count().ShouldEqual(1);
    [Fact] void should_keep_each_declarations_local_support() => _result.Diagnostics.Where(IsWarning).Select(diagnostic => diagnostic.Facts.Length).ShouldEqual([2, 2]);

    GenerationFact[] InScope(string producer, string name)
    {
        var facts = CoOwner(producer);
        var scope = new BackendScopeKey { Store = new SubjectId { Value = $"dotnet://Ordering/{name}Store" } };
        return
        [
            .. facts.Select(fact => fact is ArtifactDeclarationFact declaration
                ? declaration with { Definition = declaration.Definition with { Artifact = declaration.Definition.Artifact with { Scope = scope } } }
                : fact),
            new BackendScopeFact
            {
                Id = new FactId { Value = $"{producer}:scope" }, Subject = scope.Store, Evidence = facts[0].Evidence,
                Definition = new BackendScopeDefinition { Key = scope, Name = name }
            }
        ];
    }
}
