// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class DeclarationInputMemberAdmissionValidator
{
    public static void Member(DeclarationMemberKey member, GenerationFact fact, string path, AdapterContributionAdmissionContext context)
    {
        if (!AdapterContributionText.IsNormalized(member.Declaration.Value, true) || member.Declaration != fact.Subject)
        {
            Invalid(fact, $"{path}.Definition.Member.Declaration", "Declaration must equal the exact fact subject", context);
        }

        if (!ValidName(member.Name))
        {
            Invalid(fact, $"{path}.Definition.Member.Name", "Member name must be a normalized Screenplay identifier", context);
        }

        BackendScopeFactAdmissionValidator.ValidateKey(member.Scope, $"{path}.Definition.Member.Scope", (fact.Id, fact.Subject), context);
    }

    public static void Validate(GenerationFact fact, string path, AdapterContributionAdmissionContext context)
    {
        switch (fact)
        {
            case DeclarationInputMemberDeclarationFact declaration:
                Member(declaration.Definition.Member, declaration, path, context);
                if (declaration.Definition.DeclarationOrder < 0)
                {
                    context.Add(AdapterContributionAdmissionDiagnosticCode.InvalidDeclarationOrder, $"{path}.Definition.DeclarationOrder", "Declaration order must be nonnegative", fact.Id, fact.Subject);
                }

                break;
            case DeclarationInputMemberTypeUseFact typeUse:
                Member(typeUse.Definition.Member, typeUse, path, context);
                GranularFactAdmissionValidator.Validate(ArtifactTypeUse(typeUse), path, context);
                break;
            case EntryPointInputTransferFact transfer:
                Transfer(transfer, path, context);
                break;
        }
    }

    static ArtifactMemberTypeUseFact ArtifactTypeUse(DeclarationInputMemberTypeUseFact fact) => new()
    {
        Id = fact.Id,
        Subject = fact.Subject,
        Evidence = fact.Evidence,
        Definition = new ArtifactMemberTypeUseDefinition
        {
            Member = new ArtifactMemberKey
            {
                Artifact = new ArtifactKey { Subject = fact.Subject, Kind = ArtifactKind.Query, Scope = fact.Definition.Member.Scope },
                Name = fact.Definition.Member.Name
            },
            Type = fact.Definition.Type
        }
    };

    static bool ValidName(string? name) => AdapterContributionText.IsNormalized(name, false) &&
        (char.IsLetter(name![0]) || name[0] == '_') && name.All(character => char.IsLetterOrDigit(character) || character == '_');

    static void Invalid(GenerationFact fact, string path, string message, AdapterContributionAdmissionContext context) =>
        context.Add(AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch, path, message, fact.Id, fact.Subject);

    static void Transfer(EntryPointInputTransferFact fact, string path, AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        if (!AdapterContributionText.IsNormalized(definition.Method.Value, true) || definition.Method != fact.Subject)
        {
            Invalid(fact, $"{path}.Definition.Method", "Method must equal the exact fact subject", context);
        }

        AdapterContributionAdmissionValidator.ValidateArtifactKey(definition.Artifact, $"{path}.Definition.Artifact", fact.Id, context);
        if (definition.Artifact.Kind != ArtifactKind.Query || definition.Kind != RelationshipKind.Reads)
        {
            Invalid(fact, $"{path}.Definition", "Only query input transfer from Reads is supported", context);
        }
    }
}
