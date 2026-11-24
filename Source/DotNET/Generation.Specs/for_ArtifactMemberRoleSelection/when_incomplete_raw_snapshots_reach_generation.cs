// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_incomplete_raw_snapshots_reach_generation : given.role_candidates
{
    GeneratedScreenplayDefinition _result = null!;
    void Because()
    {
        var malformed = Candidate("first:bad", "firstId", 1);
        _result = Generator.Generate(Modern(malformed with { Definition = malformed.Definition with { Diagnostics = [null!] } }), new() { Domain = "Ordering" });
    }
    [Fact] void should_report_a_local_error_without_throwing() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.InvalidArtifactMemberRoleSelection);
    [Fact] void should_not_derive_a_role() => _result.AdapterRun.Derivation!.Facts.Select(record => record.Fact).OfType<ArtifactMemberRoleFact>().ShouldBeEmpty();
}
