// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_reading_adapter_contract_discriminators : Specification
{
    [Fact] void should_define_every_new_unknown_discriminator_as_minus_one()
    {
        ((int)AdapterSourceLanguage.Unknown).ShouldEqual(-1);
        ((int)AdapterCategory.Unknown).ShouldEqual(-1);
        ((int)AdapterHostCapability.Unknown).ShouldEqual(-1);
        ((int)GenerationFactCapability.Unknown).ShouldEqual(-1);
        ((int)TypeUseShapeKind.Unknown).ShouldEqual(-1);
        ((int)ArtifactMemberRoleKind.Unknown).ShouldEqual(-1);
        ((int)AdapterRunDisposition.Unknown).ShouldEqual(-1);
        ((int)GenerationFactDisposition.Unknown).ShouldEqual(-1);
        ((int)AdapterContributionAdmissionDiagnosticCode.Unknown).ShouldEqual(-1);
        ((int)GenerationDiagnosticSeverity.Unknown).ShouldEqual(-1);
    }

    [Fact] void should_append_granular_fact_capabilities_without_renumbering_existing_values()
    {
        ((int)GenerationFactCapability.SpecificationValue).ShouldEqual(8);
        ((int)GenerationFactCapability.ArtifactDeclaration).ShouldEqual(9);
        ((int)GenerationFactCapability.ArtifactMemberDeclaration).ShouldEqual(10);
        ((int)GenerationFactCapability.ArtifactMemberTypeUse).ShouldEqual(11);
        ((int)GenerationFactCapability.TypeUseBinding).ShouldEqual(12);
        ((int)GenerationFactCapability.ArtifactMemberRole).ShouldEqual(13);
    }

    [Fact] void should_preserve_existing_diagnostic_severity_values()
    {
        ((int)GenerationDiagnosticSeverity.Information).ShouldEqual(0);
        ((int)GenerationDiagnosticSeverity.Warning).ShouldEqual(1);
        ((int)GenerationDiagnosticSeverity.Error).ShouldEqual(2);
    }
}
