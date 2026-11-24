// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_ordered_member_role_selections : for_ArtifactMemberRoleSelection.given.role_candidates
{
    AdapterContributionAdmissionResult _admitted = null!;
    AdapterContributionAdmissionResult _missingCapability = null!;
    AdapterContributionAdmissionResult _invalid = null!;
    AdapterContributionAdmissionResult _foreignPayload = null!;
    AdapterContributionAdmissionResult _fabricatedLineage = null!;
    AdapterContributionAdmissionResult _wrongScope = null!;
    void Because()
    {
        var diagnostics = new List<GenerationDiagnostic> { Warning() };
        var candidate = Candidate("first:candidate", null, 1) with { Definition = Candidate("first:candidate", null, 1).Definition with { Diagnostics = diagnostics } };
        _admitted = Admit(candidate);
        diagnostics.Clear();
        _missingCapability = Admit(candidate, GenerationFactCapability.ArtifactMemberRole);
        _invalid = Admit(candidate with { Definition = candidate.Definition with { ObservationOrder = -1 } });
        _foreignPayload = Admit(candidate with { Definition = candidate.Definition with { Diagnostics = [Warning() with { Facts = [new() { Value = "foreign:fact" }] }] } });
        _fabricatedLineage = Admit(candidate with { Definition = candidate.Definition with { Diagnostics = [Warning() with { Lineage = new() { Producer = new() { Id = "forged", Version = "1.0.0" } } }] } });
        var support = Support(candidate, [new() { Method = Method, Scope = new() { Store = new() { Value = "source://store/other" } } }]);
        var descriptor = Descriptor(GenerationFactCapability.ArtifactMemberRoleSelection, GenerationFactCapability.MethodFactSupport);
        _wrongScope = AdapterContributionAdmission.Admit(descriptor, new() { Adapter = First, Facts = [candidate, support] });
    }
    [Fact] void should_admit_a_source_independent_no_member_alternative() => _admitted.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_the_payload_before_producer_mutation() => ((ArtifactMemberRoleSelectionFact)_admitted.Snapshot!.Facts.Single()).Definition.Diagnostics.Single().Code.ShouldEqual("OWNED");
    [Fact] void should_require_the_new_capability() => _missingCapability.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_negative_observation_order() => _invalid.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_foreign_diagnostic_fact() => _foreignPayload.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_adapter_fabricated_lineage() => _fabricatedLineage.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_support_for_a_different_backend_scope() => _wrongScope.IsAdmitted.ShouldBeFalse();

    static AdapterDescriptor Descriptor(params GenerationFactCapability[] capabilities) => new() { Identity = First, SourceLanguage = AdapterSourceLanguage.SourceIndependent, Category = AdapterCategory.ApplicationFramework, EmittedFactCapabilities = [.. capabilities] };
    static AdapterContributionAdmissionResult Admit(ArtifactMemberRoleSelectionFact candidate, GenerationFactCapability capability = GenerationFactCapability.ArtifactMemberRoleSelection) => AdapterContributionAdmission.Admit(Descriptor(capability), new() { Adapter = First, Facts = [candidate] });
}
