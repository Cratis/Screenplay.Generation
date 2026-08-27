// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.when_admitting_malformed_contributions;

public class with_nonportable_source_range_paths : given.a_contribution
{
    static readonly string[] _invalidPaths =
    [
        "/checkout/Code.cs",
        "C:/checkout/Code.cs",
        "Folder\\Code.cs",
        "../Code.cs",
        "./Code.cs",
        "Folder//Code.cs",
        "%2e/Code.cs",
        "%2e%2e/Code.cs"
    ];

    IReadOnlyDictionary<string, AdapterContributionAdmissionResult> _results = null!;
    AdapterContributionAdmissionResult _valid = null!;

    void Because()
    {
        _results = _invalidPaths.ToDictionary(path => path, AdmitWithPath, StringComparer.Ordinal);
        _valid = AdmitWithPath("Accounts/Registration/Register.cs");
    }

    [Fact] void should_reject_every_nonportable_display_path() => _results.Values.All(result => !result.IsAdmitted).ShouldBeTrue();
    [Fact] void should_report_every_nonportable_display_path_as_an_invalid_source_range() => _results.Values.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.InvalidSourceRange)).ShouldBeTrue();
    [Fact] void should_continue_to_admit_a_normalized_relative_display_path() => _valid.IsAdmitted.ShouldBeTrue();

    static AdapterContributionAdmissionResult AdmitWithPath(string path)
    {
        var fact = EveryFact().OfType<ArtifactFact>().Single();
        var source = fact.Evidence.Source! with { Path = path };
        var contribution = Contribution(
            facts:
            [
                fact with
                {
                    Evidence = fact.Evidence with { Source = source }
                }
            ]);
        return Admit(Descriptor(GenerationFactCapability.Artifact), contribution);
    }
}
