// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.given;

public class declaration_input_members : method_eligibility
{
    protected static DeclarationInputMemberDeclarationFact Surface() => new()
    {
        Id = Id("surface"),
        Subject = ArtifactSubject,
        Evidence = Evidence(),
        Definition = new DeclarationInputMemberDeclarationDefinition
        {
            Member = new DeclarationMemberKey { Declaration = ArtifactSubject, Name = "filter" },
            DeclarationOrder = 0
        }
    };

    protected static DeclarationInputMemberTypeUseFact SurfaceType(IReadOnlyList<TypeUseShapeKind>? shape = null) => new()
    {
        Id = Id("surface-type"),
        Subject = ArtifactSubject,
        Evidence = Evidence(),
        Definition = new DeclarationInputMemberTypeUseDefinition
        {
            Member = Surface().Definition.Member,
            Type = new TypeUseDefinition { Name = "string", Shape = shape ?? [TypeUseShapeKind.Named] }
        }
    };

    protected static EntryPointInputTransferFact Transfer() => new()
    {
        Id = Id("transfer"),
        Subject = ArtifactSubject,
        Evidence = Evidence(),
        Definition = new EntryPointInputTransferDefinition { Method = ArtifactSubject, Artifact = new ArtifactKey { Subject = ArtifactSubject, Kind = ArtifactKind.Query }, Kind = RelationshipKind.Reads }
    };
}
