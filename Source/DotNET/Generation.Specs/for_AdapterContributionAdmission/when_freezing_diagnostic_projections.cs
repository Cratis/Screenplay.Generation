// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_freezing_diagnostic_projections : given.diagnostic_candidates
{
    MethodDiagnosticProjectionFact _frozen;
    string _before;
    MethodDiagnosticProjectionFact _original;
    MethodFactSupportFact _support;
    void Because()
    {
        var intent = _original = Intent();
        var alternatives = new List<MethodFactSupportAlternative> { new() { Method = Method } };
        var support = Support() with { Definition = Support().Definition with { Alternatives = alternatives } };
        var result = Admit(intent, support);
        result.IsAdmitted.ShouldBeTrue();
        _frozen = (MethodDiagnosticProjectionFact)result.Snapshot.Facts.Single(fact => fact.Id == intent.Id);
        _support = (MethodFactSupportFact)result.Snapshot.Facts.Single(fact => fact.Id == support.Id);
        _before = JsonSerializer.Serialize(_frozen);
        alternatives.Clear();
    }
    [Fact] void should_preserve_the_concrete_contract_on_round_trip() => JsonSerializer.Serialize(JsonSerializer.Deserialize<MethodDiagnosticProjectionFact>(_before)).ShouldEqual(_before);
    [Fact] void should_copy_the_payload_without_caller_owned_objects() => ReferenceEquals(_frozen.Definition.Diagnostic, _original.Definition.Diagnostic).ShouldBeFalse();
    [Fact] void should_keep_frozen_support_when_the_caller_mutates() => _support.Definition.Alternatives.Count.ShouldEqual(1);
}
