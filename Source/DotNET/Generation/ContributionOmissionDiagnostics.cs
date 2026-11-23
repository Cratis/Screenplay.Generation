// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class ContributionOmissionDiagnostics
{
    public static ImmutableArray<GenerationDiagnostic> Coalesce(EffectiveGenerationInputs effective, GenerationDerivationSnapshot derivation, ResolvedApplicationGraph graph, ScreenplayLoweringCoverage coverage, ImmutableArray<GenerationDiagnostic> diagnostics)
    {
        if (!effective.OriginalFacts.OfType<OmittedFactDiagnosticGroupFact>().Any())
        {
            return diagnostics;
        }

        var inputs = effective.OriginalFacts.Select(fact => new GenerationFactRecord { Fact = fact }).Concat(derivation.Facts).ToArray();
        var records = GenerationFactDispositionCalculator.Calculate(
            inputs.Where(record => !effective.HasDisposition(record.Fact)),
            graph,
            coverage,
            [.. diagnostics.Where(diagnostic => !effective.Diagnostics.Any(eligibility => Canonical.Diagnostic(eligibility) == Canonical.Diagnostic(diagnostic)))])
            .Concat(inputs.Where(record => effective.HasDisposition(record.Fact)).Select(record => record with
            {
                Disposition = effective.DispositionOf(record.Fact),
                Diagnostics = [.. effective.Diagnostics.Where(diagnostic => diagnostic.Facts.Contains(record.Fact.Id))]
            })).ToArray();
        var groupDiagnostics = records.Where(record => record.Fact is OmittedFactDiagnosticGroupFact).SelectMany(record => record.Diagnostics);
        return OmissionDiagnosticCoalescer.Coalesce([.. records], [.. diagnostics.Concat(groupDiagnostics)]);
    }
}
