// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_a_library_usage_has_no_exact_entry_point : given.an_artifact_usage
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts().Where(fact => fact is not EntryPointFact), Entry(method: new SubjectId { Value = "dotnet://OtherProject/Api.Find``1(System.String)" })]);

    [Fact] void should_not_join_a_same_named_method_in_another_project() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_keep_a_non_entry_method_as_valid_provenance() => _result.Diagnostics.ShouldBeEmpty();
}
