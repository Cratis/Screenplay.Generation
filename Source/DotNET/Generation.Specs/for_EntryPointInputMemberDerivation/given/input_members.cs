// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation.given;

public class input_members : for_MethodFactEligibility.given.method_facts
{
    protected static DeclarationMemberKey Member(string name = "filter", SubjectId? declaration = null) => new() { Declaration = declaration ?? Declaration, Name = name };

    protected static DeclarationInputMemberDeclarationFact Surface(string name = "filter", int order = 0, SubjectId? declaration = null) => new()
    {
        Id = new FactId { Value = $"surface:{declaration?.Value}:{name}" },
        Subject = declaration ?? Declaration,
        Evidence = UsageEvidence(ConceptAdapter),
        Definition = new DeclarationInputMemberDeclarationDefinition { Member = Member(name, declaration), DeclarationOrder = order }
    };

    protected static DeclarationInputMemberTypeUseFact SurfaceType(string name = "filter", TypeUseDefinition? type = null, SubjectId? declaration = null) => new()
    {
        Id = new FactId { Value = $"surface-type:{declaration?.Value}:{name}" },
        Subject = declaration ?? Declaration,
        Evidence = UsageEvidence(ConceptAdapter),
        Definition = new DeclarationInputMemberTypeUseDefinition { Member = Member(name, declaration), Type = type ?? Scalar() }
    };

    protected static TypeUseDefinition Scalar() => new() { Name = "string", Shape = [TypeUseShapeKind.Named] };

    protected static EntryPointInputTransferFact Transfer() => new()
    {
        Id = new FactId { Value = "input-transfer" },
        Subject = Method,
        Evidence = UsageEvidence(ApplicationAdapter),
        Definition = new EntryPointInputTransferDefinition { Method = Method, Artifact = Query with { Kind = ArtifactKind.Query }, Kind = RelationshipKind.Reads }
    };

    protected static MethodArtifactUsageFact Execution(int? order = 0) => Usage() with { Definition = Usage().Definition with { ExecutionOrder = order } };

    protected static GenerationFact[] InputFacts(params GenerationFact[] additions) =>
        [.. CompleteFacts().Where(fact => fact is not MethodArtifactUsageFact).Select(QueryEndpoint), Execution(), Surface(), SurfaceType(), Transfer(), .. additions];

    protected static ArtifactMemberDeclarationFact[] Members(GenerationDerivationSnapshot result) => [.. result.Facts.Select(record => record.Fact).OfType<ArtifactMemberDeclarationFact>()];

    static GenerationFact QueryEndpoint(GenerationFact fact) => fact switch
    {
        ArtifactDeclarationFact declaration when declaration.Definition.Artifact == Query => declaration with { Definition = declaration.Definition with { Artifact = Transfer().Definition.Artifact } },
        EntryPointFact entry => entry with { Definition = entry.Definition with { Artifact = Transfer().Definition.Artifact } },
        _ => fact
    };
}
