// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class TypeUseTargetOmissionDescription
{
    public static SubjectId DiagnosticSubject(GenerationFact fact, SubjectId fallback) =>
        fact switch
        {
            TypeUseBindingPolicyFact policy => policy.Definition.Member.Artifact.Subject,
            TypeUseTargetOmittedFact omission => omission.Definition.Member.Artifact.Subject,
            DeclarationInputMemberTargetOmittedFact omission => omission.Definition.Member.Declaration,
            _ => fallback
        };

    public static string Family(GenerationFact fact, string fallback) => fact switch
    {
        TypeUseBindingPolicyFact => "type-use binding policy",
        TypeUseTargetOmittedFact or DeclarationInputMemberTargetOmittedFact => "type-use target omission",
        _ => fallback
    };
}
