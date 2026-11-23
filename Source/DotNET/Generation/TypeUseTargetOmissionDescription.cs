// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class TypeUseTargetOmissionDescription
{
    public static SubjectId DiagnosticSubject(GenerationFact fact, SubjectId fallback) =>
        fact is TypeUseTargetOmittedFact omission ? omission.Definition.Member.Artifact.Subject : fallback;

    public static string Family(GenerationFact fact, string fallback) =>
        fact is TypeUseTargetOmittedFact ? "type-use target omission" : fallback;
}
