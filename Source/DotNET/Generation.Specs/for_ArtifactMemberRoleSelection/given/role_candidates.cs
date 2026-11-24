// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection.given;

public class role_candidates : for_ScreenplayDefinitionGenerator.given.a_generator
{
    protected static readonly AdapterIdentity First = new() { Id = "first", Version = "1.0.0" };
    protected static readonly AdapterIdentity Second = new() { Id = "second", Version = "1.0.0" };
    protected static readonly SubjectId Subject = new() { Value = "source://Orders/Submit" };
    protected static readonly SubjectId Method = new() { Value = "source://Orders/Configure" };
    protected static readonly ArtifactKey Artifact = new() { Subject = Subject, Kind = ArtifactKind.Command };
    protected static readonly Evidence Proof = new() { Adapter = First, Strength = EvidenceStrength.Exact };
    protected GeneratedScreenplayDefinition Forward = null!;
    protected GeneratedScreenplayDefinition Reverse = null!;

    protected static readonly SubjectId EventSubject = new() { Value = "source://Orders/Submitted" };
    protected static readonly ArtifactKey EventArtifact = new() { Subject = EventSubject, Kind = ArtifactKind.Event };

    public static AdapterRunSnapshot Capture(IEnumerable<AdapterContribution> contributions) => Snapshot([.. contributions.Select(contribution => Completed(contribution.Adapter, contribution.Facts))]);

    protected static GenerationFact[] Declarations() =>
    [
        new ArtifactFact
        {
            Id = new() { Value = "first:artifact" }, Subject = Subject, Evidence = Proof,
            Definition = new() { Key = Artifact, Name = "SubmitOrder", Properties = [Property("firstId", "Uuid"), Property("secondId", "Uuid"), Property("thirdId", "Uuid")] }
        },
        new ArtifactPlacementFact
        {
            Id = new() { Value = "first:placement" }, Subject = Subject, Evidence = Proof, Artifact = Artifact,
            Placement = new() { Module = "Orders", Slice = "Submit", SliceKind = GenerationSliceKind.StateChange }
        },
        new ArtifactFact { Id = new() { Value = "first:event" }, Subject = EventSubject, Evidence = Proof, Definition = new() { Key = EventArtifact, Name = "OrderSubmitted", Properties = [Property("firstId", "Uuid")] } },
        new ArtifactPlacementFact { Id = new() { Value = "first:event-placement" }, Subject = EventSubject, Evidence = Proof, Artifact = EventArtifact, Placement = new() { Module = "Orders", Slice = "Submit", SliceKind = GenerationSliceKind.StateChange } },
        new RelationshipFact { Id = new() { Value = "first:produces" }, Subject = Subject, Evidence = Proof, Definition = new() { Key = new() { Kind = RelationshipKind.Produces, Source = Subject, Target = EventSubject } } }
    ];

    protected static ArtifactMemberRoleSelectionFact Candidate(string id, string? member, long order, int priority = 200, AdapterIdentity? producer = null, params GenerationDiagnostic[] diagnostics) => new()
    {
        Id = new() { Value = id }, Subject = Subject, Evidence = Proof with { Adapter = producer ?? First },
        Definition = new()
        {
            Artifact = Artifact, Role = ArtifactMemberRoleKind.EventSourceIdentifier,
            Member = member is null ? null : new() { Artifact = Artifact, Name = member },
            Priority = priority, ObservationOrder = order, Diagnostics = diagnostics
        }
    };

    protected static GenerationDiagnostic Warning(string code = "OWNED") => new() { Code = code, Severity = GenerationDiagnosticSeverity.Warning, Message = "Configured identity is unresolved", Subject = Subject };

    protected static MethodFactSupportFact Support(ArtifactMemberRoleSelectionFact candidate, params MethodFactSupportAlternative[] alternatives) => new()
    {
        Id = new() { Value = $"{candidate.Id.Value}:support" }, Subject = candidate.Subject, Evidence = candidate.Evidence,
        Definition = new() { Candidate = candidate.Id, Alternatives = alternatives.Length == 0 ? [new() { Method = Method }] : alternatives }
    };

    protected static MethodExecutionClassificationFact Failure(SubjectId? method = null) => new()
    {
        Id = new() { Value = "frontend:failure" }, Subject = method ?? Method,
        Evidence = Proof with { Adapter = new() { Id = "frontend", Version = "1.0.0" } },
        Definition = new()
        {
            Method = method ?? Method, Kind = MethodExecutionKind.FailureOnly,
            OmissionReason = Warning("FAILURE_ONLY") with { Source = new() { Path = "Orders.cs", StartLine = 1, StartColumn = 1, EndLine = 1, EndColumn = 2 } }
        }
    };

    protected static AdapterContribution[] Inputs(params GenerationFact[] facts) => [.. Declarations().Concat(facts).GroupBy(fact => fact.Evidence.Adapter).Select(group => new AdapterContribution { Adapter = group.Key, Facts = [.. group] })];

    protected void GenerateBoth(params GenerationFact[] facts)
    {
        var contributions = Inputs(facts);
        Forward = Generator.Generate(Capture(contributions), new() { Domain = "Ordering" });
        Reverse = Generator.Generate(Capture(contributions.Reverse().Select(contribution => contribution with { Facts = [.. contribution.Facts.Reverse()] })), new() { Domain = "Ordering" });
    }

    protected static AdapterRunSnapshot Modern(params GenerationFact[] facts) => Capture(Inputs(facts));

    protected static BackendScopeFact ScopeFact(BackendScopeKey key) => new() { Id = new() { Value = $"first:scope:{key.EventModelName}" }, Subject = key.Store, Evidence = Proof with { Source = new() { Path = "Orders.cs", StartLine = 1, StartColumn = 1, EndLine = 1, EndColumn = 2 } }, Definition = new() { Key = key, Name = key.EventModelName ?? "Store" } };

    protected static ArtifactMemberRoleSelectionFact Scoped(ArtifactMemberRoleSelectionFact fact, BackendScopeKey scope)
    {
        var artifact = fact.Definition.Artifact with { Scope = scope };
        return fact with { Definition = fact.Definition with { Artifact = artifact, Member = fact.Definition.Member is { } member ? member with { Artifact = artifact } : null } };
    }

    protected static ArtifactMemberRoleFact[] Selected(params GenerationFact[] facts) => [.. selection_result.FromContributions(Inputs(facts)).Roles];
}
