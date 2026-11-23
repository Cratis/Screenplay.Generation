// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class TypeUseTargetOmissions
{
    public static TypeUseTargetOmittedFact[] ForMissingTarget(
        IReadOnlyList<ArtifactMemberTypeUseFact> typeUses,
        IEnumerable<TypeUseTargetOmittedFact> omissions,
        IEnumerable<ArtifactKey> declared)
    {
        var targets = declared.ToArray();
        var candidates = omissions.Where(omission => typeUses.Any(typeUse => Matches(omission, typeUse)))
            .OrderBy(omission => omission.Id.Value, StringComparer.Ordinal).ToArray();
        return typeUses.Count > 0 && typeUses.All(typeUse =>
            typeUse.Definition.Type.ObservedTypeSubject is { } subject &&
            !targets.Any(target => target.Subject == subject && target.Scope == typeUse.Definition.Type.ObservedScope) &&
            candidates.Any(omission => Matches(omission, typeUse)))
            ? candidates
            : [];
    }

    static bool Matches(TypeUseTargetOmittedFact omission, ArtifactMemberTypeUseFact typeUse) =>
        omission.Definition.Member == typeUse.Definition.Member &&
        omission.Subject == typeUse.Subject && omission.Evidence.Adapter == typeUse.Evidence.Adapter;
}
