// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class TypeUseTargetOmissionDescription
{
    public static SubjectId DiagnosticSubject(GenerationFact fact, SubjectId fallback) =>
        fact switch
        {
            TypeUseTargetOmittedFact omission => omission.Definition.Member.Artifact.Subject,
            DeclarationInputMemberTargetOmittedFact omission => omission.Definition.Member.Declaration,
            _ => fallback
        };

    public static string Family(GenerationFact fact, string fallback) =>
        fact is TypeUseTargetOmittedFact or DeclarationInputMemberTargetOmittedFact ? "type-use target omission" : fallback;
}
