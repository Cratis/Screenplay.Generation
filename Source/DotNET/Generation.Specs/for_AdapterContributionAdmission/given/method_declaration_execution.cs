// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.given;

public class method_declaration_execution : declaration_input_members
{
    protected static MethodDeclarationExecutionFact Execution(int? order = 0) => new()
    {
        Id = Id("input-only-execution"), Subject = ArtifactSubject, Evidence = Evidence(),
        Definition = new MethodDeclarationExecutionDefinition { Method = ArtifactSubject, Declaration = ExternalSubject, ExecutionOrder = order }
    };

    protected static AdapterContributionAdmissionResult AdmitExecution(MethodDeclarationExecutionFact execution) => Admit(
        descriptor: Descriptor(GenerationFactCapability.MethodDeclarationExecution), contribution: Contribution([execution]));
}
