// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_a_usage_declaration_conflicts_with_an_independent_proof : given.an_artifact_usage
{
    GenerationDerivationSnapshot _forward;
    GenerationDerivationSnapshot _reverse;

    void Because()
    {
        var declaration = new SubjectId { Value = "dotnet://CustomerLibrary/Queries.ZindCustomer" };
        var other = Document with { Subject = new SubjectId { Value = "dotnet://CustomerLibrary/Models.OtherCustomer" } };
        var facts = CompleteFacts().Concat<GenerationFact>([
            new ArtifactUsageDeclarationFact
            {
                Id = Id("library", "conflict-first"), Subject = declaration, Evidence = At(Library),
                Definition = new ArtifactUsageDeclarationDefinition { Declaration = declaration, Artifact = Document }
            },
            new ArtifactUsageDeclarationFact
            {
                Id = Id("library", "conflict-second"), Subject = declaration, Evidence = At(Library),
                Definition = new ArtifactUsageDeclarationDefinition { Declaration = declaration, Artifact = other }
            },
            new MethodArtifactUsageFact
            {
                Id = Id("application", "conflicting-usage"), Subject = Method, Evidence = At(Application),
                Definition = new MethodArtifactUsageDefinition { Method = Method, Declaration = declaration }
            }
        ]).ToArray();
        _forward = Derive(facts);
        _reverse = Derive(facts.Reverse());
    }

    [Fact] void should_preserve_the_independently_proved_reads_edge() => ((RelationshipFact)_forward.Facts.Single().Fact).Definition.Key.Target.ShouldEqual(Document.Subject);
    [Fact] void should_keep_the_valid_proof_in_the_edge_lineage() => _forward.Facts.Single().Lineage.Inputs.Select(id => id.Value).ShouldContain("library:declaration");
    [Fact] void should_not_include_the_conflicting_usage_in_the_valid_proof() => _forward.Facts.Single().Lineage.Inputs.Select(id => id.Value).ShouldNotContain("application:conflicting-usage");
    [Fact] void should_still_report_the_other_declarations_conflict() => _forward.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.ConflictingArtifactUsageMapping);
    [Fact] void should_keep_conflict_inputs_local_to_the_conflicting_declaration() => _forward.Diagnostics.Single().Facts.Select(id => id.Value).ShouldNotContain("library:declaration");
    [Fact] void should_be_independent_of_input_order() => System.Text.Json.JsonSerializer.Serialize(_forward).ShouldEqual(System.Text.Json.JsonSerializer.Serialize(_reverse));
}
