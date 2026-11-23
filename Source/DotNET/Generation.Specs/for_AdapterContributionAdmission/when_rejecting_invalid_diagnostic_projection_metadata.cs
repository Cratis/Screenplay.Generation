// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_rejecting_invalid_diagnostic_projection_metadata : given.diagnostic_candidates
{
    MethodDiagnosticProjectionFact _intent;
    void Establish() => _intent = Intent();

    [Fact] void should_reject_unknown_mode() => Admit(_intent with { Definition = _intent.Definition with { Mode = MethodDiagnosticProjectionMode.Unknown } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_undefined_mode() => Admit(_intent with { Definition = _intent.Definition with { Mode = (MethodDiagnosticProjectionMode)99 } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_non_entry_role() => Admit(_intent with { Definition = _intent.Definition with { EntryPointRole = ArtifactKind.Event } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_inexact_intent_evidence() => Admit(_intent with { Evidence = _intent.Evidence with { Strength = EvidenceStrength.Heuristic } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_different_subject() => Admit(_intent with { Subject = ConceptSubject }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_missing_definition() => Admit(_intent with { Definition = null! }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_missing_payload() => Admit(_intent with { Definition = _intent.Definition with { Diagnostic = null! } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_missing_capability() => Admit(_intent, descriptor: Descriptor() with { EmittedFactCapabilities = [GenerationFactCapability.ArtifactDeclaration, GenerationFactCapability.MethodFactSupport] }).IsAdmitted.ShouldBeFalse();
}
