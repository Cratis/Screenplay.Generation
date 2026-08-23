// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_ordering_mixed_legacy_and_identity_diagnostics : given.facts
{
    ResolvedApplicationGraph _forward = null!;
    ResolvedApplicationGraph _reverse = null!;

    void Because()
    {
        var firstLegacy = LegacyDiagnostic("CODE\u001fShared.cs", "Order\u001fMessages.cs");
        var secondLegacy = LegacyDiagnostic("CODE", "Shared.cs\u001fOrder\u001fMessages.cs");
        var identity = IdentityDiagnostic();
        _forward = new GenerationResolver().Resolve([ContributionWith(firstLegacy, identity, secondLegacy)]);
        _reverse = new GenerationResolver().Resolve([ContributionWith(secondLegacy, identity, firstLegacy)]);
    }

    [Fact] void should_retain_all_structurally_distinct_diagnostics() => _forward.Diagnostics.Count.ShouldEqual(3);
    [Fact] void should_order_mixed_diagnostics_independently_of_input_order() => JsonSerializer.Serialize(_reverse.Diagnostics).ShouldEqual(JsonSerializer.Serialize(_forward.Diagnostics));

    static AdapterContribution ContributionWith(params GenerationDiagnostic[] diagnostics) => new()
    {
        Adapter = new AdapterIdentity { Id = "adapter\u001fidentity", Version = "1\u001f0" },
        Diagnostics = diagnostics
    };

    static GenerationDiagnostic LegacyDiagnostic(string code, string displayPath) => new()
    {
        Code = code,
        Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = GenerationDiagnosticOutcome.Unsupported,
        Message = "Shared\u001fdiagnostic",
        Source = new SourceRange
        {
            Path = displayPath,
            StartLine = 1,
            StartColumn = 1,
            EndLine = 1,
            EndColumn = 10
        },
        Subject = EventSubject
    };

    static GenerationDiagnostic IdentityDiagnostic() => new()
    {
        Code = "IDENTITY\u001f0001",
        Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = GenerationDiagnosticOutcome.Unsupported,
        Message = "Identity\u001fdiagnostic",
        Source = new SourceRange
        {
            Path = "Identity\u001fDisplay.cs",
            FileIdentity = new SourceFileIdentity
            {
                Project = "Banking\u001fIdentity",
                Path = "Common\u001fOrder.cs"
            },
            StartLine = 2,
            StartColumn = 3,
            EndLine = 4,
            EndColumn = 5
        },
        Subject = EventSubject
    };
}
