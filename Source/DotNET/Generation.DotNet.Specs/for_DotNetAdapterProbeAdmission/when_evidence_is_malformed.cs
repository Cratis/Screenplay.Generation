// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterProbeAdmission;

public class when_evidence_is_malformed : given.a_probe_admission_context
{
    Dictionary<string, string> _outcomes = null!;
    AdapterRunRecord _unknownSource = null!;
    AdapterRunRecord _generatedPathSource = null!;
    AdapterRunRecord _nullItem = null!;
    AdapterRunRecord _blockedWithBadEvidence = null!;
    AdapterRunRecord _blockedWithBadDiagnostics = null!;
    AdapterRunRecord _blockedWithoutDiagnostics = null!;
    AdapterRunRecord _blockedWithDefaultDiagnostics = null!;

    void Establish()
    {
        _outcomes = new Dictionary<string, string>
        {
            ["empty description"] = Outcome(Evidence("")),
            ["padded description"] = Outcome(Evidence(" padded")),
            ["control in description"] = Outcome(Evidence("tab\there")),
            ["decomposed description"] = Outcome(Evidence("e\u0301")),
            ["lone surrogate description"] = Outcome(Evidence("bad\ud800")),
            ["whitespace in capability"] = Outcome(Evidence("fine", capability: "a b")),
            ["padded capability"] = Outcome(Evidence("fine", capability: "a ")),
            ["empty capability"] = Outcome(Evidence("fine", capability: "")),
            ["relative subject"] = Outcome(Evidence("fine", subject: "relative/path")),
            ["empty subject"] = Outcome(Evidence("fine", subject: "")),
            ["backslash subject"] = Outcome(Evidence("fine", subject: "dotnet://Specs\\Type")),
            ["whitespace subject"] = Outcome(Evidence("fine", subject: "urn:a b")),
            ["dot segment subject"] = Outcome(Evidence("fine", subject: "dotnet://Specs/./Type")),
            ["dot dot segment subject"] = Outcome(Evidence("fine", subject: "dotnet://Specs/a/../Type")),
            ["encoded dot dot subject"] = Outcome(Evidence("fine", subject: "dotnet://Specs/%2e%2E/Type")),
            ["opaque dot dot subject"] = Outcome(Evidence("fine", subject: "urn:a/../b")),
            ["dot segment before query subject"] = Outcome(Evidence("fine", subject: "dotnet://Specs/a/..?x=1")),
            ["host only subject"] = Outcome(Evidence("fine", subject: "https://host")),
            ["dot segment in query only subject"] = Outcome(Evidence("fine", subject: "dotnet://Specs/a?x=/../")),
            ["dot segment in fragment only subject"] = Outcome(Evidence("fine", subject: "dotnet://Specs/a#/../")),
            ["plain subject"] = Outcome(Evidence("fine", subject: "dotnet://Specs/Type"))
        };

        var project = Project();
        var unknown = RangeIn(project, 1, 2);
        _unknownSource = Probe(Applicable(Evidence("fine", source: new SourceRange { Path = "Missing.cs", StartLine = 1, StartColumn = 1, EndLine = 1, EndColumn = 2 })), project);
        _generatedPathSource = Probe(Applicable(Evidence("fine", source: unknown with { EndColumn = 99 })), project);
        _nullItem = Probe(new AdapterProbeApplicable { Evidence = [Evidence("fine"), null!] });
        _blockedWithBadEvidence = Probe(new AdapterProbeBlocked
        {
            Evidence = [Evidence(" bad")],
            Diagnostics = [new GenerationDiagnostic { Code = "PROBE", Severity = GenerationDiagnosticSeverity.Error, Message = "blocked" }]
        });
        _blockedWithBadDiagnostics = Probe(new AdapterProbeBlocked
        {
            Diagnostics = [new GenerationDiagnostic { Code = "PROBE", Severity = GenerationDiagnosticSeverity.Unknown, Message = "blocked" }]
        });
        _blockedWithoutDiagnostics = Probe(new AdapterProbeBlocked { Diagnostics = [] });
        _blockedWithDefaultDiagnostics = Probe(new AdapterProbeBlocked { Diagnostics = default });
    }

    [Fact] void should_reject_an_empty_description() => _outcomes["empty description"].ShouldEqual(Rejected);
    [Fact] void should_reject_a_padded_description() => _outcomes["padded description"].ShouldEqual(Rejected);
    [Fact] void should_reject_a_control_character_in_the_description() => _outcomes["control in description"].ShouldEqual(Rejected);
    [Fact] void should_reject_a_description_that_is_not_in_normalization_form_c() => _outcomes["decomposed description"].ShouldEqual(Rejected);
    [Fact] void should_reject_a_description_with_a_lone_surrogate() => _outcomes["lone surrogate description"].ShouldEqual(Rejected);
    [Fact] void should_reject_whitespace_inside_a_capability_id() => _outcomes["whitespace in capability"].ShouldEqual(Rejected);
    [Fact] void should_reject_a_padded_capability_id() => _outcomes["padded capability"].ShouldEqual(Rejected);
    [Fact] void should_reject_an_empty_capability_id() => _outcomes["empty capability"].ShouldEqual(Rejected);
    [Fact] void should_reject_a_relative_subject() => _outcomes["relative subject"].ShouldEqual(Rejected);
    [Fact] void should_reject_an_empty_subject() => _outcomes["empty subject"].ShouldEqual(Rejected);
    [Fact] void should_reject_a_subject_with_a_backslash() => _outcomes["backslash subject"].ShouldEqual(Rejected);
    [Fact] void should_reject_a_subject_with_whitespace() => _outcomes["whitespace subject"].ShouldEqual(Rejected);
    [Fact] void should_reject_a_dot_segment_in_a_subject() => _outcomes["dot segment subject"].ShouldEqual(Rejected);
    [Fact] void should_reject_a_dot_dot_segment_in_a_subject() => _outcomes["dot dot segment subject"].ShouldEqual(Rejected);
    [Fact] void should_reject_an_encoded_dot_dot_segment_in_a_subject() => _outcomes["encoded dot dot subject"].ShouldEqual(Rejected);
    [Fact] void should_reject_a_dot_dot_segment_in_an_opaque_subject() => _outcomes["opaque dot dot subject"].ShouldEqual(Rejected);
    [Fact] void should_reject_a_dot_dot_segment_before_a_query() => _outcomes["dot segment before query subject"].ShouldEqual(Rejected);
    [Fact] void should_admit_a_host_only_subject() => _outcomes["host only subject"].ShouldEqual("admitted");
    [Fact] void should_ignore_dot_segments_inside_a_query() => _outcomes["dot segment in query only subject"].ShouldEqual("admitted");
    [Fact] void should_ignore_dot_segments_inside_a_fragment() => _outcomes["dot segment in fragment only subject"].ShouldEqual("admitted");
    [Fact] void should_admit_a_plain_subject() => _outcomes["plain subject"].ShouldEqual("admitted");
    [Fact] void should_reject_a_source_in_no_authored_file() => Diagnostics(_unknownSource.Probe).ShouldEqual(Rejected);
    [Fact] void should_reject_a_source_with_coordinates_outside_the_file() => Diagnostics(_generatedPathSource.Probe).ShouldEqual(Rejected);
    [Fact] void should_reject_a_null_evidence_item() => Diagnostics(_nullItem.Probe).ShouldEqual(Rejected);
    [Fact] void should_reject_a_blocked_probe_whose_evidence_is_malformed() => Diagnostics(_blockedWithBadEvidence.Probe).ShouldEqual(Rejected);
    [Fact] void should_reject_a_blocked_probe_whose_diagnostics_are_malformed() => Diagnostics(_blockedWithBadDiagnostics.Probe).ShouldEqual(Rejected);
    [Fact] void should_reject_a_blocked_probe_without_diagnostics() => Diagnostics(_blockedWithoutDiagnostics.Probe).ShouldEqual(Rejected);
    [Fact] void should_reject_a_blocked_probe_with_default_diagnostics() => Diagnostics(_blockedWithDefaultDiagnostics.Probe).ShouldEqual(Rejected);
}
