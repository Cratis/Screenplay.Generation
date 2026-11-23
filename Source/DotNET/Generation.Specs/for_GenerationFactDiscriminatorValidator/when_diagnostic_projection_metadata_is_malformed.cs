// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDiscriminatorValidator;

public class when_diagnostic_projection_metadata_is_malformed : for_GenerationFactDerivation.given.diagnostic_projection
{
    GenerationDerivationSnapshot[] _results;
    void Because()
    {
        var intent = Intent();
        _results = [.. new[]
        {
            intent with { Definition = null! },
            intent with { Definition = intent.Definition with { Mode = MethodDiagnosticProjectionMode.Unknown } },
            intent with { Definition = intent.Definition with { Mode = (MethodDiagnosticProjectionMode)99 } },
            intent with { Definition = intent.Definition with { Diagnostic = null! } }
        }.Select(fact => Derive(fact))];
    }
    [Fact] void should_fail_closed_without_throwing() => _results.All(result => result.Diagnostics.Single().Code == GenerationDiagnosticCodes.InvalidDiagnosticIntent).ShouldBeTrue();
    [Fact] void should_not_create_derived_facts() => _results.All(result => result.Facts.IsEmpty).ShouldBeTrue();
}
