// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.given;

public class diagnostic_candidates : for_GenerationFactDerivation.given.diagnostic_projection
{
    protected static AdapterDescriptor Descriptor() => new()
    {
        Identity = Observer, SourceLanguage = AdapterSourceLanguage.CSharp, Category = AdapterCategory.EventStore,
        EmittedFactCapabilities = [GenerationFactCapability.ArtifactDeclaration, GenerationFactCapability.MethodFactSupport, GenerationFactCapability.MethodDiagnosticProjection, GenerationFactCapability.OmittedFactDiagnosticGroup]
    };

    protected static AdapterContributionAdmissionResult Admit(MethodDiagnosticProjectionFact intent, MethodFactSupportFact? support = null, IReadOnlyList<GenerationDiagnostic>? diagnostics = null, AdapterDescriptor? descriptor = null) =>
        AdapterContributionAdmission.Admit(
            descriptor ?? Descriptor(),
            new() { Adapter = Observer, Facts = [Observation(), intent, support ?? Support()], Diagnostics = diagnostics ?? [] },
            new Authority());

    protected sealed class Authority : ISourceAuthorityValidator
    {
        public bool IsAuthoritative(SourceRange source) => source.Path == Invocation.Path;
    }
}
