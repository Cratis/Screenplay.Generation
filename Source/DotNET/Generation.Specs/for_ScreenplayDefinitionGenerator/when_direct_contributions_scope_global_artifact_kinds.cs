// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_direct_contributions_scope_global_artifact_kinds : for_GenerationResolver.given.backend_scopes
{
    GeneratedScreenplayDefinition _result;

    void Because()
    {
        var concept = Key(ModelSubject, ArtifactKind.Concept, First);
        var query = Key(CommandSubject, ArtifactKind.Query, First);
        _result = Generate(Scope(First, "scope"), Artifact(concept, "concept", "CustomerCode"), Artifact(query, "query", "FindCustomer"), Placement(query, "query-placement"));
    }

    [Fact] void should_omit_the_scoped_concept_without_mutating_its_subject() => _result.Source.ShouldNotContain("concept CustomerCode");
    [Fact] void should_omit_the_scoped_query() => _result.Source.ShouldNotContain("query FindCustomer");
    [Fact] void should_diagnose_the_artifact_and_placement_misuses_even_without_admission() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidBackendScopeUsage).ShouldEqual(3);
}
