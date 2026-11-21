// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_an_executed_declaration_has_no_artifact_mapping : given.an_artifact_usage
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts().Where(fact => fact is not ArtifactUsageDeclarationFact)]);

    [Fact] void should_not_invent_a_relationship() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_report_the_missing_mapping() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.MissingArtifactUsageMapping);
    [Fact] void should_link_both_sides_of_the_failed_join() => _result.Diagnostics.Single().Facts.Select(id => id.Value).ShouldEqual("application:entry", "library:usage");
    [Fact] void should_retain_the_source_location() => _result.Diagnostics.Single().Source.Path.ShouldEqual("Queries/FindCustomer.cs");
}
