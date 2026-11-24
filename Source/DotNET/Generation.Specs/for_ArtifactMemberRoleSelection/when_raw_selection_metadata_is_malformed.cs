// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_raw_selection_metadata_is_malformed : given.role_candidates
{
    Dictionary<string, given.selection_result> _results = null!;
    void Because()
    {
        var candidate = Candidate("first:invalid", "secondId", 2);
        var definition = candidate.Definition;
        Dictionary<string, ArtifactMemberRoleSelectionFact> cases = new()
        {
            ["negative-order"] = candidate with { Definition = definition with { ObservationOrder = -1 } },
            ["wrong-member"] = candidate with { Definition = definition with { Member = new() { Artifact = EventArtifact, Name = "secondId" } } },
            ["blank-member"] = candidate with { Definition = definition with { Member = definition.Member! with { Name = " " } } },
            ["null-definition"] = candidate with { Definition = null! },
            ["null-diagnostics"] = candidate with { Definition = definition with { Diagnostics = null! } },
            ["null-payload"] = candidate with { Definition = definition with { Diagnostics = [null!] } },
            ["wrong-subject"] = candidate with { Subject = EventSubject },
            ["undefined-role"] = candidate with { Definition = definition with { Role = (ArtifactMemberRoleKind)999 } },
            ["missing-scope"] = Scoped(candidate, new() { Store = new() { Value = "source://missing/store" } }),
            ["foreign-payload-fact"] = candidate with { Definition = definition with { Diagnostics = [Warning() with { Facts = [new() { Value = "second:foreign" }] }] } },
            ["fabricated-lineage"] = candidate with { Definition = definition with { Diagnostics = [Warning() with { Lineage = new() { Producer = new() { Id = "forged", Version = "1.0.0" } } }] } }
        };
        _results = cases.ToDictionary(entry => entry.Key, entry => given.selection_result.FromContributions(Inputs(entry.Value)), StringComparer.Ordinal);
    }
    [Fact] void should_reject_negative_observation_order() => Rejected("negative-order");
    [Fact] void should_reject_member_occurrence_mismatch() => Rejected("wrong-member");
    [Fact] void should_reject_blank_member_names() => Rejected("blank-member");
    [Fact] void should_reject_a_missing_definition_without_throwing() => Rejected("null-definition");
    [Fact] void should_reject_null_diagnostic_collections() => Rejected("null-diagnostics");
    [Fact] void should_reject_null_diagnostic_payloads() => Rejected("null-payload");
    [Fact] void should_reject_artifact_subject_mismatch() => Rejected("wrong-subject");
    [Fact] void should_reject_undefined_roles() => Rejected("undefined-role");
    [Fact] void should_not_fall_back_from_an_undeclared_scope() => Rejected("missing-scope");
    [Fact] void should_reject_foreign_diagnostic_dependencies() => Rejected("foreign-payload-fact");
    [Fact] void should_reject_adapter_fabricated_lineage() => Rejected("fabricated-lineage");
    void Rejected(string name) => _results[name].RoleSelectionRules.Single().Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.InvalidArtifactMemberRoleSelection);
}
