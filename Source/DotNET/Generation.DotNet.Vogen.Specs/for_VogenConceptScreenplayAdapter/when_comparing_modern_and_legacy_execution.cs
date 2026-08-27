// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_comparing_modern_and_legacy_execution : given.a_vogen_compilation
{
    string _modernFacts = null!;
    string _legacyFacts = null!;
    string _modernDiagnostics = null!;
    string _legacyDiagnostics = null!;
    byte[] _modernFactBytes = null!;
    byte[] _legacyFactBytes = null!;
    byte[] _modernDiagnosticBytes = null!;
    byte[] _legacyDiagnosticBytes = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            "Concepts",
            new SourceFile(
                "/checkout/Concepts/CustomerCode.cs",
                """
                namespace Concepts;
                [Vogen.ValueObject<string>]
                public partial struct CustomerCode
                {
                    private static Vogen.Validation Validate(string value) => Vogen.Validation.Invalid("Required");
                }
                """));
        var context = new DotNetAnalysisContext([MappedProject("Concepts.Project", "concepts-project", compilation)]);
        var modernAdapter = new VogenConceptScreenplayAdapter();
        var legacyAdapter = new VogenConceptScreenplayAdapter();
        IDescribedDotNetScreenplayAdapter modernInterface = modernAdapter;
        IDotNetScreenplayAdapter legacyInterface = legacyAdapter;
        var rawModern = modernInterface.Analyze(context, new DotNetAdapterOptions());
        var rawLegacy = legacyInterface.Analyze(context, new DotNetAdapterOptions());
        var modern = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(modernAdapter)],
            context,
            new DotNetAdapterOptions());
        var legacy = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.ForLegacy(legacyAdapter)],
            context,
            new DotNetAdapterOptions());
        _modernFacts = Facts(modern);
        _legacyFacts = Facts(legacy);
        _modernDiagnostics = Diagnostics(modern);
        _legacyDiagnostics = Diagnostics(legacy);
        _modernFactBytes = JsonSerializer.SerializeToUtf8Bytes(rawModern.Facts.Cast<object>().ToArray());
        _legacyFactBytes = JsonSerializer.SerializeToUtf8Bytes(rawLegacy.Facts.Cast<object>().ToArray());
        _modernDiagnosticBytes = JsonSerializer.SerializeToUtf8Bytes(rawModern.Diagnostics);
        _legacyDiagnosticBytes = JsonSerializer.SerializeToUtf8Bytes(rawLegacy.Diagnostics);
    }

    [Fact] void should_keep_modern_and_legacy_facts_semantically_identical() => _modernFacts.ShouldEqual(_legacyFacts);
    [Fact] void should_keep_modern_and_legacy_contribution_diagnostics_identical() => _modernDiagnostics.ShouldEqual(_legacyDiagnostics);
    [Fact] void should_keep_modern_and_legacy_fact_bytes_identical() => _modernFactBytes.SequenceEqual(_legacyFactBytes).ShouldBeTrue();
    [Fact] void should_keep_modern_and_legacy_diagnostic_bytes_identical() => _modernDiagnosticBytes.SequenceEqual(_legacyDiagnosticBytes).ShouldBeTrue();

    static string Facts(AdapterRunSnapshot snapshot) => string.Join(
        '|',
        snapshot.Facts.Select(record => $"{record.Fact.GetType().Name}:{record.Fact.Id.Value}:{record.Fact.Subject.Value}:{record.Fact.Evidence.Source!.FileIdentity}"));

    static string Diagnostics(AdapterRunSnapshot snapshot) => string.Join(
        '|',
        Contribution(snapshot).Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}:{diagnostic.Source?.FileIdentity}"));

    static AdapterContributionSnapshot Contribution(AdapterRunSnapshot snapshot) =>
        ((AdapterExecutionCompleted)snapshot.Adapters.Single().Execution).Contribution;
}
