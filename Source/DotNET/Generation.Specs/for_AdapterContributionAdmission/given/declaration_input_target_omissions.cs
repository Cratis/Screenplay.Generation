// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.given;

public class declaration_input_target_omissions : declaration_input_members
{
    protected static DeclarationInputMemberTypeUseFact ObservedType() => SurfaceType() with
    {
        Definition = SurfaceType().Definition with
        {
            Type = new TypeUseDefinition { Name = "Int", Shape = [TypeUseShapeKind.Named], ObservedTypeSubject = ExternalSubject }
        }
    };

    protected static DeclarationInputMemberTargetOmittedFact Omission(GenerationDiagnostic? diagnostic = null) => new()
    {
        Id = Id("declaration-omission"),
        Subject = ArtifactSubject,
        Evidence = Evidence(),
        Definition = new DeclarationInputMemberTargetOmittedDefinition
        {
            Member = Surface().Definition.Member,
            Diagnostic = diagnostic ?? new GenerationDiagnostic
            {
                Code = "atomic.FlagsOmitted",
                Severity = GenerationDiagnosticSeverity.Warning,
                Message = "No target is contributed",
                Subject = ArtifactSubject,
                Source = Source(),
                Facts = [ObservedType().Id]
            }
        }
    };

    protected static AdapterContributionAdmissionResult AdmitOmission(params GenerationFact[] facts) => Admit(
        descriptor: Descriptor([.. AllCapabilities(), GenerationFactCapability.DeclarationInputMemberTargetOmitted]),
        contribution: Contribution(facts));
}
