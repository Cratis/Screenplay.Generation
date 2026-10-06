// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_verification_is_diagnostic_free : given.a_verifying_generator
{
    void Because() => GenerateBoth();

    [Fact] void should_keep_contribution_generation_successful() => ContributionsResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_snapshot_generation_successful() => SnapshotResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_add_contribution_diagnostics() => ContributionsResult.Diagnostics.ShouldBeEmpty();
    [Fact] void should_not_add_snapshot_diagnostics() => SnapshotResult.Diagnostics.ShouldBeEmpty();
}
