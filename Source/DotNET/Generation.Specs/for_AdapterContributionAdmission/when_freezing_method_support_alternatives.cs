// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_freezing_method_support_alternatives : given.method_eligibility
{
    GenerationFact[] _facts;
    List<SubjectId> _ancestors;
    List<MethodFactSupportAlternative> _alternatives;
    AdapterContributionAdmissionResult _result;

    void Establish()
    {
        _ancestors = [ScenarioSubject];
        _alternatives = [new MethodFactSupportAlternative { Method = ArtifactSubject, EnclosingExecutables = _ancestors }];
        _facts = [Usage(), Support(_alternatives), Classification()];
    }

    void Because()
    {
        _result = AdmitEligibility(_facts);
        _ancestors.Clear();
        _alternatives.Clear();
    }

    [Fact] void should_admit_the_new_capabilities() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_alternative_membership() => _result.Snapshot.Facts.OfType<MethodFactSupportFact>().Single().Definition.Alternatives.Count.ShouldEqual(1);
    [Fact] void should_deeply_freeze_lexical_ancestors() => _result.Snapshot.Facts.OfType<MethodFactSupportFact>().Single().Definition.Alternatives.Single().EnclosingExecutables.Single().ShouldEqual(ScenarioSubject);
}
