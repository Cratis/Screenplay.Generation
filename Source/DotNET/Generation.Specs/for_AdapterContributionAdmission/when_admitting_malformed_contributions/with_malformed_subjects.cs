// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.when_admitting_malformed_contributions;

public class with_malformed_subjects : given.a_contribution
{
    AdapterContributionAdmissionResult _factSubject = null!;
    AdapterContributionAdmissionResult _nestedSubject = null!;

    void Because()
    {
        var malformedFactSubject = EveryFact();
        malformedFactSubject[0] = malformedFactSubject[0] with { Subject = new SubjectId { Value = "relative/artifact" } };
        _factSubject = Admit(contribution: Contribution(malformedFactSubject));

        var malformedNestedSubject = EveryFact();
        var artifact = (ArtifactFact)malformedNestedSubject[0];
        var properties = artifact.Definition.Properties.ToArray();
        properties[0] = properties[0] with
        {
            Type = properties[0].Type with { Subject = new SubjectId { Value = "dotnet://Accounts/bad subject" } }
        };
        malformedNestedSubject[0] = artifact with
        {
            Definition = artifact.Definition with { Properties = properties }
        };
        _nestedSubject = Admit(contribution: Contribution(malformedNestedSubject));
    }

    [Fact] void should_reject_malformed_fact_subjects_atomically() => AssertRejected(_factSubject);
    [Fact] void should_reject_malformed_nested_referenced_subjects_atomically() => AssertRejected(_nestedSubject);

    static void AssertRejected(AdapterContributionAdmissionResult result)
    {
        result.Snapshot.ShouldBeNull();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(AdapterContributionAdmissionDiagnosticCode.InvalidSubject);
    }
}
