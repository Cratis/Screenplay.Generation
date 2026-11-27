// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

/// <summary>Withholds invalid jagged-array view participants before concept-name yield can preview them.</summary>
static class JaggedArrayViewPreflight
{
    public static EffectiveGenerationInputs Select(EffectiveGenerationInputs input)
    {
        var invalid = input.Facts.Where(fact => !JaggedArrayViews.IsConsistent(fact, input.Facts)).ToArray();
        if (invalid.Length == 0)
        {
            return input;
        }

        var dispositions = input.Dispositions.ToBuilder();
        var diagnostics = input.Diagnostics.ToBuilder();
        var withheld = new HashSet<GenerationFactInstance>();
        foreach (var fact in invalid)
        {
            var projections = JaggedArrayViews.Projections(fact, input.Facts).ToArray();
            diagnostics.Add(TypeUseCompatibilityProjections.JaggedViewDiagnostic(fact, projections));
            foreach (var participant in projections.Cast<GenerationFact>().Prepend(fact))
            {
                withheld.Add(GenerationFactInstance.Of(participant));
                dispositions[GenerationFactInstance.Of(participant)] = GenerationFactDisposition.OmittedWithDiagnostic;
            }
        }

        return input with
        {
            BlockingFacts = [.. input.BlockingFacts, .. invalid],
            Facts = [.. input.Facts.Where(fact => !withheld.Contains(GenerationFactInstance.Of(fact)))],
            Diagnostics = [.. diagnostics.OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)],
            Dispositions = dispositions.ToImmutable()
        };
    }
}
