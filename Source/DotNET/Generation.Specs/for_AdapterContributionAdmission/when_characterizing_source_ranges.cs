// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_characterizing_source_ranges : given.a_characterized_admission
{
    static readonly string[] _paths =
    [
        "Accounts/Register.cs",
        "/rooted.cs",
        "C:/drive.cs",
        "c:drive.cs",
        "a\\b.cs",
        "../up.cs",
        "./here.cs",
        "a/./b.cs",
        "a//b.cs",
        "a/",
        "%2e/x.cs",
        "%252e%252e/x.cs",
        "a%2Fb.cs",
        "a%5Cb.cs",
        "%43:/x.cs",
        "a%zz/b.cs",
        "a b.cs",
        "e\u0301.cs",
        "",
        " "
    ];

    string[] _observed = null!;

    void Because()
    {
        var cases = new List<(string, AdapterContributionAdmissionResult)>();
        foreach (var path in _paths)
        {
            cases.Add(($"path[{path}]", AdmitSource(source => source with { Path = path })));
        }

        cases.Add(("identity-path", AdmitSource(source => source with { FileIdentity = source.FileIdentity! with { Path = "../x.cs" } })));
        cases.Add(("identity-project", AdmitSource(source => source with { FileIdentity = source.FileIdentity! with { Project = " " } })));
        cases.Add(("identity-null", AdmitSource(source => source with { FileIdentity = null })));
        cases.Add(("zero-start-line", AdmitSource(source => source with { StartLine = 0 })));
        cases.Add(("zero-start-column", AdmitSource(source => source with { StartColumn = 0 })));
        cases.Add(("zero-end-line", AdmitSource(source => source with { EndLine = 0 })));
        cases.Add(("zero-end-column", AdmitSource(source => source with { EndColumn = 0 })));
        cases.Add(("end-before-start-line", AdmitSource(source => source with { StartLine = 5, EndLine = 4 })));
        cases.Add(("end-before-start-column", AdmitSource(source => source with { StartColumn = 9, EndColumn = 8 })));
        cases.Add(("later-end-line-smaller-column", AdmitSource(source => source with { StartLine = 3, StartColumn = 9, EndLine = 4, EndColumn = 1 })));
        cases.Add(("invalid-range-and-invalid-path", AdmitSource(source => source with { Path = "../x.cs", StartLine = 0 })));
        cases.Add(("no-authority-validator", AdmitSourceWith(null, source => source)));
        cases.Add(("non-authoritative", AdmitSourceWith(new RejectingValidator(), source => source)));
        cases.Add(("invalid-beats-authority", AdmitSourceWith(new RejectingValidator(), source => source with { StartLine = 0 })));
        cases.Add(("diagnostic-source-invalid", AdmitDiagnosticSource(source => source with { Path = "/x.cs" }, null)));
        cases.Add(("diagnostic-source-no-authority", AdmitDiagnosticSource(source => source, null)));
        cases.Add(("diagnostic-source-non-authoritative", AdmitDiagnosticSource(source => source, new RejectingValidator())));
        cases.Add(("diagnostic-source-with-subject", AdmitDiagnosticSource(source => source with { EndColumn = 0 }, null, ArtifactSubject)));
        cases.Add(("diagnostic-source-accepted", AdmitDiagnosticSource(source => source, AcceptingValidator())));
        _observed = [.. RenderSource(cases).Select(line => line.Replace("\u0301", "<U+0301>", StringComparison.Ordinal))];
    }

    [Fact] void should_report_the_source_shapes_in_order() => string.Join('\n', _observed).ShouldEqual(Expected);

    const string Expected = """
path[Accounts/Register.cs] => admitted
path[/rooted.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|/rooted.cs@3:1-3:20
path[C:/drive.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|C:/drive.cs@3:1-3:20
path[c:drive.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|c:drive.cs@3:1-3:20
path[a\b.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|a\b.cs@3:1-3:20
path[../up.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|../up.cs@3:1-3:20
path[./here.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|./here.cs@3:1-3:20
path[a/./b.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|a/./b.cs@3:1-3:20
path[a//b.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|a//b.cs@3:1-3:20
path[a/] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|a/@3:1-3:20
path[%2e/x.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|%2e/x.cs@3:1-3:20
path[%252e%252e/x.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|%252e%252e/x.cs@3:1-3:20
path[a%2Fb.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|a%2Fb.cs@3:1-3:20
path[a%5Cb.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|a%5Cb.cs@3:1-3:20
path[%43:/x.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|%43:/x.cs@3:1-3:20
path[a%zz/b.cs] => admitted
path[a b.cs] => admitted
path[e<U+0301>.cs] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|e<U+0301>.cs@3:1-3:20
path[] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|@3:1-3:20
path[ ] => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact| @3:1-3:20
identity-path => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|Accounts/../x.cs/Accounts/Register.cs@3:1-3:20
identity-project => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact| /Accounts/Register.cs/Accounts/Register.cs@3:1-3:20
identity-null => admitted
zero-start-line => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|Accounts/Register.cs@0:1-3:20
zero-start-column => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|Accounts/Register.cs@3:0-3:20
zero-end-line => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|Accounts/Register.cs@3:1-0:20
zero-end-column => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|Accounts/Register.cs@3:1-3:0
end-before-start-line => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|Accounts/Register.cs@5:1-4:20
end-before-start-column => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|Accounts/Register.cs@3:9-3:8
later-end-line-smaller-column => admitted
invalid-range-and-invalid-path => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|../x.cs@0:1-3:20
no-authority-validator => SourceAuthorityRequired|[artifact].Evidence.Source|Source range 'Accounts/Register.cs:3:1' requires a host authority validator|dotnet://Accounts/artifact|Accounts/Register.cs@3:1-3:20
non-authoritative => SourceNotAuthoritative|[artifact].Evidence.Source|Source range 'Accounts/Register.cs:3:1' is not authoritative authored source|dotnet://Accounts/artifact|Accounts/Register.cs@3:1-3:20
invalid-beats-authority => InvalidSourceRange|[artifact].Evidence.Source|(portable-range)|dotnet://Accounts/artifact|Accounts/Register.cs@0:1-3:20
diagnostic-source-invalid => InvalidSourceRange|Contribution.Diagnostics[0].Source|(portable-range)||/x.cs@3:1-3:20
diagnostic-source-no-authority => SourceAuthorityRequired|Contribution.Diagnostics[0].Source|Source range 'Accounts/Register.cs:3:1' requires a host authority validator||Accounts/Register.cs@3:1-3:20
diagnostic-source-non-authoritative => SourceNotAuthoritative|Contribution.Diagnostics[0].Source|Source range 'Accounts/Register.cs:3:1' is not authoritative authored source||Accounts/Register.cs@3:1-3:20
diagnostic-source-with-subject => InvalidSourceRange|Contribution.Diagnostics[0].Source|(portable-range)|dotnet://Accounts/artifact|Accounts/Register.cs@3:1-3:0
diagnostic-source-accepted => admitted
""";
    [Fact] void should_describe_the_portable_range_rule_in_the_message() => PortableRangeMessage.ShouldEqual(" must identify a normalized portable relative path without rooted, backslash, empty, or dot segments and an ordered positive 1-based range");

    static AdapterContributionAdmissionResult AdmitSource(Func<SourceRange, SourceRange> change) =>
        AdmitSourceWith(AcceptingValidator(), change);

    static AdapterContributionAdmissionResult AdmitSourceWith(ISourceAuthorityValidator? validator, Func<SourceRange, SourceRange> change)
    {
        var fact = EveryFact().OfType<ArtifactFact>().Single();
        var changed = fact with { Evidence = fact.Evidence with { Source = change(fact.Evidence.Source!) } };
        return AdapterContributionAdmission.Admit(Descriptor(GenerationFactCapability.Artifact), Contribution([changed]), validator);
    }

    static AdapterContributionAdmissionResult AdmitDiagnosticSource(
        Func<SourceRange, SourceRange> change,
        ISourceAuthorityValidator? validator,
        SubjectId? subject = null)
    {
        var diagnostic = new GenerationDiagnostic
        {
            Code = "ATOMIC0001",
            Severity = GenerationDiagnosticSeverity.Warning,
            Message = "Something",
            Source = change(Source()),
            Subject = subject
        };
        return AdapterContributionAdmission.Admit(Descriptor(), Contribution([], [diagnostic]), validator);
    }

    static ISourceAuthorityValidator AcceptingValidator() => new AcceptingSourceValidator();

    sealed class AcceptingSourceValidator : ISourceAuthorityValidator
    {
        public bool IsAuthoritative(SourceRange source) => true;
    }

    sealed class RejectingValidator : ISourceAuthorityValidator
    {
        public bool IsAuthoritative(SourceRange source) => false;
    }
}
