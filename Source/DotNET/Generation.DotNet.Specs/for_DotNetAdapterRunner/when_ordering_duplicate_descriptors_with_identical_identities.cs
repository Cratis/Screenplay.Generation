// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_ordering_duplicate_descriptors_with_identical_identities : given.a_runner_context
{
    ModernAdapter[] _forwardAdapters = null!;
    ModernAdapter[] _reverseAdapters = null!;
    string _forward = null!;
    string _reverse = null!;

    void Because()
    {
        _forwardAdapters = Adapters();
        _reverseAdapters = Adapters();
        _forward = Projection(DotNetAdapterRunner.Run(
            _forwardAdapters.Select(DotNetAdapterRegistration.For),
            new DotNetAnalysisContext([]),
            Options));
        _reverse = Projection(DotNetAdapterRunner.Run(
            _reverseAdapters.AsEnumerable().Reverse().Select(DotNetAdapterRegistration.For),
            new DotNetAnalysisContext([]),
            Options));
    }

    [Fact] void should_use_the_full_frozen_descriptor_as_the_duplicate_tie_breaker() => _forward.ShouldEqual("Concepts:AuthoredSource:vogen.alpha:Artifact|Validation:SemanticAnalysis:vogen.zeta:ConceptValidationRule");
    [Fact] void should_keep_duplicate_records_identical_under_reversed_input() => _reverse.ShouldEqual(_forward);
    [Fact] void should_reject_every_duplicate_before_probe_or_analyze() => _forwardAdapters.Concat(_reverseAdapters).All(adapter => adapter.ProbeCount == 0 && adapter.AnalyzeCount == 0).ShouldBeTrue();

    static ModernAdapter[] Adapters() =>
    [
        new(Descriptor(
            "duplicate",
            category: AdapterCategory.Validation,
            hostCapabilities: [AdapterHostCapability.SemanticAnalysis],
            apiCapabilities: [new AdapterApiCapability { Id = "vogen.zeta" }],
            factCapabilities: [GenerationFactCapability.ConceptValidationRule],
            generationVersions: new GenerationVersionRange
            {
                MinimumInclusive = new Version(1, 0, 0),
                MaximumExclusive = new Version(3, 0, 0)
            })),
        new(Descriptor(
            "duplicate",
            category: AdapterCategory.Concepts,
            hostCapabilities: [AdapterHostCapability.AuthoredSource],
            apiCapabilities: [new AdapterApiCapability { Id = "vogen.alpha" }],
            factCapabilities: [GenerationFactCapability.Artifact],
            generationVersions: new GenerationVersionRange
            {
                MinimumInclusive = new Version(1, 0, 0),
                MaximumExclusive = new Version(2, 0, 0)
            }))
    ];

    static string Projection(AdapterRunSnapshot snapshot) => string.Join(
        '|',
        snapshot.Adapters.Select(record =>
            $"{record.Descriptor.Category}:" +
            $"{string.Join(',', record.Descriptor.RequiredHostCapabilities)}:" +
            $"{string.Join(',', record.Descriptor.RequiredApiCapabilities.Select(capability => capability.Id))}:" +
            $"{string.Join(',', record.Descriptor.EmittedFactCapabilities)}"));
}
