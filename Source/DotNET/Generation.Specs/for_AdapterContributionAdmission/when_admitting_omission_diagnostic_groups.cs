// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_omission_diagnostic_groups : given.diagnostic_candidates
{
    OmittedFactDiagnosticGroupFact _group;
    AdapterContributionAdmissionResult _result;
    List<FactId> _covered;
    void Establish()
    {
        _covered = [Observation().Id];
        _group = new() { Id = Id("group"), Subject = Method, Evidence = ObservationEvidence(), Definition = new() { CoveredFacts = _covered, Diagnostic = Payload() } };
    }
    void Because()
    {
        _result = AdmitGroup(_group);
        _covered.Clear();
        _group = _group with { Definition = _group.Definition with { CoveredFacts = [Observation().Id] } };
    }
    [Fact] void should_admit_valid_local_coverage() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_caller_owned_coverage() => ((OmittedFactDiagnosticGroupFact)_result.Snapshot.Facts.Single(fact => fact.Id == _group.Id)).Definition.CoveredFacts.Count.ShouldEqual(1);
    [Fact] void should_round_trip_the_public_contract() => JsonSerializer.Deserialize<OmittedFactDiagnosticGroupFact>(JsonSerializer.Serialize(_group)).Definition.Diagnostic.Code.ShouldEqual(Payload().Code);
    [Fact] void should_reject_empty_coverage() => AdmitGroup(_group with { Definition = _group.Definition with { CoveredFacts = [] } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_duplicate_coverage() => AdmitGroup(_group with { Definition = _group.Definition with { CoveredFacts = [Observation().Id, Observation().Id] } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_self_coverage() => AdmitGroup(_group with { Definition = _group.Definition with { CoveredFacts = [_group.Id] } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_unknown_coverage() => AdmitGroup(_group with { Definition = _group.Definition with { CoveredFacts = [Id("missing")] } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_foreign_coverage() => AdmitGroup(_group with { Definition = _group.Definition with { CoveredFacts = [new() { Value = "foreign:fact" }] } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_overlapping_groups() => AdmitGroup(_group with { Definition = _group.Definition with { CoveredFacts = [Observation().Id] } }, _group with { Id = Id("overlap"), Definition = _group.Definition with { CoveredFacts = [Observation().Id] } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_missing_payloads() => AdmitGroup(_group with { Definition = _group.Definition with { Diagnostic = null! } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_missing_definitions() => AdmitGroup(_group with { Definition = null! }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_unlocated_payloads() => AdmitGroup(_group with { Definition = _group.Definition with { Diagnostic = Payload() with { Source = null } } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_inexact_group_evidence() => AdmitGroup(_group with { Evidence = ObservationEvidence() with { Strength = EvidenceStrength.Heuristic } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_require_the_group_capability() => AdmitGroup(_group, descriptor: Descriptor() with { EmittedFactCapabilities = [GenerationFactCapability.ArtifactDeclaration] }).IsAdmitted.ShouldBeFalse();

    static FactId Id(string value) => new() { Value = $"observations:{value}" };

    static AdapterContributionAdmissionResult AdmitGroup(OmittedFactDiagnosticGroupFact group, OmittedFactDiagnosticGroupFact? other = null, AdapterDescriptor? descriptor = null) =>
        AdapterContributionAdmission.Admit(
            descriptor ?? Descriptor(),
            new() { Adapter = Observer, Facts = other is null ? [Observation(), group] : [Observation(), group, other] },
            new Authority());
}
