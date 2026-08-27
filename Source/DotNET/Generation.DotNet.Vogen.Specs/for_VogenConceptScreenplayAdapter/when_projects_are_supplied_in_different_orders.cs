// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_projects_are_supplied_in_different_orders : given.a_vogen_compilation
{
    static readonly JsonSerializerOptions _serializerOptions = new();

    AdapterContribution _first = null!;
    byte[] _firstDiagnosticsJson = null!;
    byte[] _firstFactsJson = null!;
    byte[] _secondDiagnosticsJson = null!;
    byte[] _secondFactsJson = null!;

    void Because()
    {
        var firstProject = Project(
            "Project.A",
            CompilationFrom(
                "Shared",
                new SourceFile(
                    "/workspace/A/Code.cs",
                    """
                    namespace Shared;
                    [Vogen.ValueObject<System.Guid>]
                    public partial struct Code
                    {
                        private static Vogen.Validation Validate(System.Guid value) => Vogen.Validation.Invalid("Project A code is invalid");
                    }
                    """)));
        var secondProject = Project(
            "Project.B",
            CompilationFrom(
                "Shared",
                new SourceFile(
                    "/workspace/B/Code.cs",
                    """
                    namespace Shared;
                    [Vogen.Instance("Unknown", "00000000-0000-0000-0000-000000000000")]
                    [Vogen.ValueObject<System.Guid>]
                    public partial struct Code
                    {
                        private static Vogen.Validation Validate(System.Guid value) => Vogen.Validation.Ok;
                        private static System.Guid NormalizeInput(System.Guid value) => value;
                    }
                    """)));

        var options = new DotNetAdapterOptions
        {
            FeatureRoot = "Source",
            NamespaceSegmentsToSkip = 1
        };
        var adapter = new VogenConceptScreenplayAdapter();
        _first = adapter.Analyze(new DotNetAnalysisContext([firstProject, secondProject]), options);
        var second = adapter.Analyze(new DotNetAnalysisContext([secondProject, firstProject]), options);
        _firstFactsJson = JsonSerializer.SerializeToUtf8Bytes(_first.Facts.Cast<object>().ToArray(), _serializerOptions);
        _secondFactsJson = JsonSerializer.SerializeToUtf8Bytes(second.Facts.Cast<object>().ToArray(), _serializerOptions);
        _firstDiagnosticsJson = JsonSerializer.SerializeToUtf8Bytes(_first.Diagnostics, _serializerOptions);
        _secondDiagnosticsJson = JsonSerializer.SerializeToUtf8Bytes(second.Diagnostics, _serializerOptions);
    }

    [Fact] void should_keep_fact_json_byte_deterministic() => _firstFactsJson.SequenceEqual(_secondFactsJson).ShouldBeTrue();
    [Fact] void should_keep_diagnostic_json_byte_deterministic() => _firstDiagnosticsJson.SequenceEqual(_secondDiagnosticsJson).ShouldBeTrue();
    [Fact] void should_keep_fact_json_bytes_stable() => Hash(_firstFactsJson).ShouldEqual("A90824EAE8A0487FE3BB380BA8493DF7C173260D20F8DCEB1BEE4A4A0865E14C");
    [Fact] void should_keep_diagnostic_json_bytes_stable() => Hash(_firstDiagnosticsJson).ShouldEqual("2E11A67968DA865CA18FEC0AA04F2A521E8BFCBB85010652DDE8DFF71980F5E7");
    [Fact] void should_keep_same_named_types_as_distinct_subjects() => _first.Facts.OfType<ArtifactFact>().Select(_ => _.Subject.Value).ShouldContainOnly("dotnet://Project.A/Shared/Shared.Code", "dotnet://Project.B/Shared/Shared.Code");
    [Fact] void should_assign_unique_deterministic_fact_ids() => _first.Facts.Select(_ => _.Id.Value).Distinct(StringComparer.Ordinal).Count().ShouldEqual(6);
    [Fact] void should_keep_exact_authored_evidence_for_both_subjects() => _first.Facts.All(_ => _.Evidence.Strength == EvidenceStrength.Exact && _.Evidence.Source is not null).ShouldBeTrue();
    [Fact] void should_keep_both_authored_validation_rules() => _first.Facts.OfType<ConceptValidationRuleFact>().Select(_ => _.Definition.Predicate).ShouldEqual(["Validate", "Validate"]);
    [Fact] void should_keep_loss_diagnostics_deterministic() => _first.Diagnostics.Select(_ => _.Code).ShouldEqual([VogenGenerationDiagnosticCodes.InputNormalizationNotRepresented, VogenGenerationDiagnosticCodes.NamedInstanceNotRepresented]);

    static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
