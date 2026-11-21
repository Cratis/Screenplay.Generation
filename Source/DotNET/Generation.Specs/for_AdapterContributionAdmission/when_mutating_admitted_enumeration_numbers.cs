// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_mutating_admitted_enumeration_numbers : given.a_numbered_enumeration
{
    AdapterContributionAdmissionResult _result = null!;

    void Because()
    {
        var numbers = new List<int> { -1, 42 };
        _result = Admit(contribution: Contribution([Representation(numbers)]));
        numbers[0] = 100;
        numbers.Clear();
    }

    [Fact] void should_admit_the_numbered_enumeration() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_retain_the_original_numbers() => _result.Snapshot.Facts.OfType<ConceptRepresentationFact>().Single().Definition.EnumerationNumbers.ShouldEqual(-1, 42);
}
