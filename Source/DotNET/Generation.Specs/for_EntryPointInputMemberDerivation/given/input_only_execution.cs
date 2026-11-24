// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation.given;

public class input_only_execution : input_members
{
    protected static MethodDeclarationExecutionFact InputExecution(int? order = 0) => new()
    {
        Id = new FactId { Value = "input-only-execution" },
        Subject = Method,
        Evidence = UsageEvidence(ConceptAdapter),
        Definition = new MethodDeclarationExecutionDefinition { Method = Method, Declaration = Declaration, ExecutionOrder = order }
    };

    protected static GenerationFact[] InputOnlyFacts(params GenerationFact[] additions) =>
        [.. InputFacts().Where(fact => fact is not (MethodArtifactUsageFact or ArtifactUsageDeclarationFact)), InputExecution(), .. additions];

    protected static BackendScopeKey Scope(string name) => new() { Store = new SubjectId { Value = $"store:{name}" }, EventModelName = name };

    protected static BackendScopeFact Backend(BackendScopeKey scope) => new()
    {
        Id = new FactId { Value = $"scope:{scope.Store.Value}" }, Subject = scope.Store, Evidence = UsageEvidence(),
        Definition = new BackendScopeDefinition { Key = scope, Name = "Inputs" }
    };

    protected static ArtifactPlacementFact Place(ArtifactKey artifact) => new()
    {
        Id = new FactId { Value = $"placement:{artifact.Subject.Value}" }, Subject = artifact.Subject, Evidence = UsageEvidence(), Artifact = artifact,
        Placement = new ArtifactPlacement { Module = "Customers", Features = ["Listing"], Slice = "FindCustomer", SliceKind = GenerationSliceKind.StateView }
    };

    protected static RelationshipFact ReturnDocument() => new()
    {
        Id = new FactId { Value = "query-returns-document" }, Subject = Transfer().Definition.Artifact.Subject, Evidence = UsageEvidence(),
        Definition = new RelationshipDefinition { Key = new RelationshipKey { Kind = RelationshipKind.Returns, Source = Transfer().Definition.Artifact.Subject, Target = Document.Subject } }
    };
}
