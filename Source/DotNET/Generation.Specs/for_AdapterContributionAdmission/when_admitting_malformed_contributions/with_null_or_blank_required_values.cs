// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.when_admitting_malformed_contributions;

public class with_null_or_blank_required_values : given.a_contribution
{
    AdapterContributionAdmissionResult _blank = null!;
    AdapterContributionAdmissionResult _nullList = null!;
    AdapterContributionAdmissionResult _nullNestedList = null!;

    void Because()
    {
        var blankFacts = EveryFact();
        var blankArtifact = (ArtifactFact)blankFacts[0];
        blankFacts[0] = blankArtifact with
        {
            Definition = blankArtifact.Definition with { Name = " " }
        };
        _blank = Admit(contribution: Contribution(blankFacts));

        _nullList = Admit(contribution: Contribution() with { Facts = null! });

        var nullNestedFacts = EveryFact();
        var nullNestedArtifact = (ArtifactFact)nullNestedFacts[0];
        nullNestedFacts[0] = nullNestedArtifact with
        {
            Definition = nullNestedArtifact.Definition with { Properties = null! }
        };
        _nullNestedList = Admit(contribution: Contribution(nullNestedFacts));
    }

    [Fact] void should_reject_blank_required_names_atomically() => AssertRejected(_blank, AdapterContributionAdmissionDiagnosticCode.MissingRequiredValue);
    [Fact] void should_reject_null_required_contribution_lists_atomically() => AssertRejected(_nullList, AdapterContributionAdmissionDiagnosticCode.NullRequiredCollection);
    [Fact] void should_reject_null_required_nested_lists_atomically() => AssertRejected(_nullNestedList, AdapterContributionAdmissionDiagnosticCode.NullRequiredCollection);

    static void AssertRejected(AdapterContributionAdmissionResult result, AdapterContributionAdmissionDiagnosticCode code)
    {
        result.Snapshot.ShouldBeNull();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);
    }
}
