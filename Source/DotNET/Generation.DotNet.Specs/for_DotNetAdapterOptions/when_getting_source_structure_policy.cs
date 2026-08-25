// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterOptions;

public class when_getting_source_structure_policy : Specification
{
    DotNetSourceStructurePolicy _policy = null!;

    void Because() => _policy = new DotNetAdapterOptions
    {
        FeatureRoot = "Source",
        Module = "Application",
        NamespaceSegmentsToSkip = 2
    }.SourceStructurePolicy;

    [Fact] void should_preserve_the_feature_root() => _policy.FeatureRoot.ShouldEqual("Source");
    [Fact] void should_preserve_the_module_collapse() => _policy.Module.ShouldEqual("Application");
    [Fact] void should_preserve_the_namespace_segments_to_skip() => _policy.NamespaceSegmentsToSkip.ShouldEqual(2);
    [Fact] void should_use_the_supported_policy_version() => _policy.Version.ShouldEqual(1);
}
