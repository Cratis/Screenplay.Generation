// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_freezing_occurrence_omission_candidates : given.occurrence_candidates
{
    OccurrenceOmissionDiagnosticFact _original;
    OccurrenceOmissionDiagnosticFact _frozen;
    MethodFactSupportFact _support;
    string _json;
    void Because()
    {
        _original = Candidate("registration");
        var alternatives = new List<MethodFactSupportAlternative> { new() { Method = Method } };
        var support = Conditional(_original) with { Definition = Conditional(_original).Definition with { Alternatives = alternatives } };
        var result = AdmitOccurrence(_original, support);
        result.IsAdmitted.ShouldBeTrue();
        _frozen = result.Snapshot.Facts.OfType<OccurrenceOmissionDiagnosticFact>().Single();
        _support = result.Snapshot.Facts.OfType<MethodFactSupportFact>().Single();
        _json = JsonSerializer.Serialize(_frozen);
        alternatives.Clear();
    }
    [Fact] void should_copy_the_family() => ReferenceEquals(_frozen.Definition.Family, _original.Definition.Family).ShouldBeFalse();
    [Fact] void should_copy_the_occurrence() => ReferenceEquals(_frozen.Definition.Occurrence, _original.Definition.Occurrence).ShouldBeFalse();
    [Fact] void should_copy_the_payload() => ReferenceEquals(_frozen.Definition.Diagnostic, _original.Definition.Diagnostic).ShouldBeFalse();
    [Fact] void should_isolate_method_support_from_caller_mutation() => _support.Definition.Alternatives.Count.ShouldEqual(1);
    [Fact] void should_round_trip_the_concrete_contract() => JsonSerializer.Serialize(JsonSerializer.Deserialize<OccurrenceOmissionDiagnosticFact>(_json)).ShouldEqual(_json);
}
