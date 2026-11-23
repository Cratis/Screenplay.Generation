// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.given;

public class occurrence_candidates : for_GenerationFactDerivation.given.occurrence_omissions
{
    protected static AdapterDescriptor OmissionDescriptor(OccurrenceOmissionDiagnosticFact candidate) => new()
    {
        Identity = candidate.Evidence.Adapter, SourceLanguage = AdapterSourceLanguage.CSharp, Category = AdapterCategory.EventStore,
        EmittedFactCapabilities = [GenerationFactCapability.OccurrenceOmissionDiagnostic, GenerationFactCapability.DeclarationInputMemberDeclaration, GenerationFactCapability.MethodFactSupport]
    };

    protected static AdapterContributionAdmissionResult AdmitOccurrence(OccurrenceOmissionDiagnosticFact candidate, params GenerationFact[] extra) =>
        AdapterContributionAdmission.Admit(
            OmissionDescriptor(candidate),
            new() { Adapter = candidate.Evidence.Adapter, Facts = [.. CandidateFacts(candidate), .. extra] },
            new OmissionAuthority());

    protected sealed class OmissionAuthority : ISourceAuthorityValidator
    {
        public bool IsAuthoritative(SourceRange source) => source.Path == "registration.cs";
    }
}
