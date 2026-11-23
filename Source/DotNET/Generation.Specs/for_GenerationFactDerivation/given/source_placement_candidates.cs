// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation.given;

public class source_placement_candidates : an_artifact_usage
{
    protected static new readonly ArtifactKey Query = new() { Subject = new SubjectId { Value = "dotnet://Application/RegisterCustomer" }, Kind = ArtifactKind.Command };
    protected static readonly ArtifactKey Event = new() { Subject = new SubjectId { Value = "dotnet://Library/Models.CustomerRegistered" }, Kind = ArtifactKind.Event };
    protected GenerationDerivationSnapshot result = null!;

    protected static ArtifactSourcePlacementCandidateFact Candidate(ArtifactKey target, ArtifactKey owner, string id, EvidenceStrength strength = EvidenceStrength.Exact) => new()
    {
        Id = new FactId { Value = $"application:{id}" }, Subject = target.Subject,
        Evidence = UsageEvidence() with { Strength = strength },
        Definition = new ArtifactSourcePlacementCandidateDefinition
        {
            Artifact = target, Owner = owner, SliceKind = GenerationSliceKind.StateChange,
            Reason = target == owner ? ArtifactSourcePlacementReason.Self : ArtifactSourcePlacementReason.ProducingCommand,
            Policy = new ArtifactSourcePlacementSelectionPolicy()
        }
    };

    protected static RelationshipFact Production(ArtifactKey owner, string id = "produces") => new()
    {
        Id = new FactId { Value = $"application:{id}" }, Subject = owner.Subject, Evidence = UsageEvidence(),
        Definition = new RelationshipDefinition
        {
            Key = new RelationshipKey { Kind = RelationshipKind.Produces, Source = owner.Subject, SourceScope = owner.Scope, Target = Event.Subject, TargetScope = Event.Scope }
        }
    };

    protected static GenerationDerivationSnapshot Resolve(params GenerationFact[] facts) => GenerationFactDerivation.Derive(
        new AdapterRunSnapshot { Facts = [.. facts.Select(fact => new GenerationFactRecord { Fact = fact })] }, new FixedResolver());

    protected static IArtifactSourcePlacementResolver Resolver() => new FixedResolver();

    protected static ArtifactPlacementFact Placement(GenerationDerivationSnapshot snapshot) => snapshot.Facts.Select(record => record.Fact).OfType<ArtifactPlacementFact>().Single();

    sealed class FixedResolver : IArtifactSourcePlacementResolver
    {
        public SourcePlacementResolutionResult Resolve(SourcePlacementResolutionRequest request) => new()
        {
            Artifact = request.Artifact,
            Placement = new ArtifactPlacement { Module = "Customers", Features = ["Registration"], Slice = request.SourceOwner == Event ? "EventSource" : "CommandSource", SliceKind = request.SliceKind }
        };
    }
}
