// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterDescriptorAdmission;

public class when_admitting_malformed_and_duplicate_api_capabilities : Specification
{
    AdapterDescriptorAdmissionResult _result = null!;

    void Because() => _result = AdapterDescriptorAdmission.Admit(new AdapterDescriptor
    {
        Identity = new AdapterIdentity { Id = "adapter", Version = "1.0.0" },
        SourceLanguage = AdapterSourceLanguage.SourceIndependent,
        Category = AdapterCategory.Concepts,
        RequiredApiCapabilities =
        [
            new AdapterApiCapability { Id = "framework.api" },
            new AdapterApiCapability { Id = " malformed " },
            new AdapterApiCapability { Id = "framework.api" }
        ]
    });

    [Fact] void should_reject_the_descriptor() => _result.IsAdmitted.ShouldBeFalse();
    [Fact] void should_report_the_malformed_capability() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.InvalidApiCapability).ShouldBeTrue();
    [Fact] void should_report_the_duplicate_capability() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.DuplicateApiCapability).ShouldBeTrue();
}
