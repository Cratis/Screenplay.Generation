// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterDescriptorAdmission;

public class when_admitting_reversed_api_capabilities : Specification
{
    AdapterDescriptor _input = null!;
    AdapterDescriptorAdmissionResult _result = null!;

    void Establish() => _input = new AdapterDescriptor
    {
        Identity = new AdapterIdentity { Id = "adapter", Version = "1.0.0" },
        SourceLanguage = AdapterSourceLanguage.SourceIndependent,
        Category = AdapterCategory.Concepts,
        RequiredApiCapabilities =
        [
            new AdapterApiCapability { Id = "framework.zeta" },
            new AdapterApiCapability { Id = "framework.alpha" }
        ]
    };

    void Because() => _result = AdapterDescriptorAdmission.Admit(_input);

    [Fact] void should_admit_the_descriptor() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_canonicalize_capabilities_by_stable_identity() => _result.Descriptor.RequiredApiCapabilities.Select(capability => capability.Id).ShouldEqual(["framework.alpha", "framework.zeta"]);
    [Fact] void should_deeply_freeze_capability_records() => ReferenceEquals(_input.RequiredApiCapabilities[0], _result.Descriptor.RequiredApiCapabilities[1]).ShouldBeFalse();
}
