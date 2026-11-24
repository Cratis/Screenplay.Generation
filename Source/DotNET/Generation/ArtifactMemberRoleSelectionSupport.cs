// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ArtifactMemberRoleSelectionSupport
{
    public static IEnumerable<ArtifactMemberRoleSelectionFact> Candidates(EffectiveGenerationInputs input)
    {
        var invalid = input.OriginalFacts.OfType<MethodFactSupportFact>().Where(support => Invalid(support, input)).Select(GenerationFactInstance.CandidateOf).ToHashSet();
        return input.OriginalFacts.OfType<ArtifactMemberRoleSelectionFact>().Where(candidate => !input.HasDisposition(candidate) || invalid.Contains(GenerationFactInstance.Of(candidate)));
    }

    public static bool Invalid(MethodFactSupportFact support, EffectiveGenerationInputs input) => input.HasDisposition(support) && input.DispositionOf(support) == GenerationFactDisposition.OmittedWithDiagnostic;

    public static IEnumerable<MethodFactSupportFact> InvalidDependencies(ArtifactMemberRoleSelectionFact candidate, EffectiveGenerationInputs input) =>
        input.OriginalFacts.OfType<MethodFactSupportFact>().Where(support => GenerationFactInstance.CandidateOf(support) == GenerationFactInstance.Of(candidate) && Invalid(support, input));
}
