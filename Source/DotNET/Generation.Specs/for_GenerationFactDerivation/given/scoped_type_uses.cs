// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation.given;

public class scoped_type_uses : for_GenerationResolver.given.backend_scopes
{
    protected static readonly ArtifactKey Owner = Key(CommandSubject, ArtifactKind.Command, First);
    protected static readonly ArtifactMemberKey Member = new() { Artifact = Owner, Name = "customer" };

    protected static GenerationFact[] Facts(BackendScopeKey? observedScope) =>
    [
        Scope(First, "first-scope"), Scope(Second, "second-scope"),
        new ArtifactDeclarationFact { Id = Id("owner"), Subject = Owner.Subject, Evidence = Evidence(), Definition = new ArtifactDeclarationDefinition { Artifact = Owner, Name = "Register" } },
        new ArtifactMemberDeclarationFact { Id = Id("member"), Subject = Owner.Subject, Evidence = Evidence(), Definition = new ArtifactMemberDeclarationDefinition { Member = Member, DeclarationOrder = 0 } },
        new ArtifactMemberTypeUseFact
        {
            Id = Id("use"), Subject = Owner.Subject, Evidence = Evidence(),
            Definition = new ArtifactMemberTypeUseDefinition { Member = Member, Type = new TypeUseDefinition { Name = "Customer", ObservedTypeSubject = ModelSubject, ObservedScope = observedScope } }
        },
        Artifact(Key(ModelSubject, ArtifactKind.ReadModel, First), "first-model", "Customer"),
        Artifact(Key(ModelSubject, ArtifactKind.ReadModel, Second), "second-model", "Customer")
    ];

    protected static GenerationDerivationSnapshot Derive(IEnumerable<GenerationFact> facts) =>
        GenerationFactDerivation.Derive(new AdapterRunSnapshot { Facts = [.. facts.Select(fact => new GenerationFactRecord { Fact = fact })] });
}
