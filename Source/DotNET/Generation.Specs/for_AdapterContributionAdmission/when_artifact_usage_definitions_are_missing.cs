// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_artifact_usage_definitions_are_missing : given.artifact_usages
{
    AdapterContributionAdmissionResult _result;

    void Because() => _result = Admit(contribution: Contribution([Declaration() with { Definition = null! }, Entry() with { Definition = null! }, Usage() with { Definition = null! }]));

    [Fact] void should_reject_without_throwing() => _result.IsAdmitted.ShouldBeFalse();
    [Fact] void should_locate_each_missing_definition() => _result.Diagnostics.Count(diagnostic => diagnostic.Path.EndsWith(".Definition", StringComparison.Ordinal)).ShouldEqual(3);
}
