// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_malformed_later_metadata_cannot_fall_back : given.role_candidates
{
    given.selection_result _result = null!;
    void Because()
    {
        var later = Candidate("second:invalid", null, -1, producer: Second);
        _result = given.selection_result.FromContributions(Inputs(Candidate("first:valid", "firstId", 1), later));
    }
    [Fact] void should_omit_the_whole_identifiable_group() => _result.SelectedRoles.ShouldBeEmpty();
    [Fact] void should_report_the_local_error() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.InvalidArtifactMemberRoleSelection);
}
