// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver.given;

public class backend_scopes : Specification
{
    protected static readonly AdapterIdentity Adapter = new() { Id = "scopes", Version = "1.0.0" };
    protected static readonly BackendScopeKey First = new() { Store = new SubjectId { Value = "dotnet://Application/Stores.FirstRegistration" }, EventModelName = "Customers" };
    protected static readonly BackendScopeKey Second = new() { Store = new SubjectId { Value = "dotnet://Application/Stores.SecondRegistration" }, EventModelName = "Customers" };
    protected static readonly SubjectId EventSubject = new() { Value = "dotnet://Library/Events.CustomerRegistered" };
    protected static readonly SubjectId ModelSubject = new() { Value = "dotnet://Library/Models.Customer" };
    protected static readonly SubjectId CommandSubject = new() { Value = "dotnet://Application/Commands.Register" };

    protected static Evidence Evidence() => new()
    {
        Adapter = Adapter,
        Strength = EvidenceStrength.Exact,
        Source = new SourceRange { Path = "Stores/Registration.cs", StartLine = 3, StartColumn = 1, EndLine = 3, EndColumn = 40 }
    };

    protected static FactId Id(string value) => new() { Value = $"scopes:{value}" };

    protected static ArtifactKey Key(SubjectId subject, ArtifactKind kind, BackendScopeKey? scope) => new() { Subject = subject, Kind = kind, Scope = scope };

    protected static BackendScopeFact Scope(BackendScopeKey key, string id) => new()
    {
        Id = Id(id),
        Subject = key.Store,
        Evidence = Evidence(),
        Definition = new BackendScopeDefinition { Key = key, Name = "Customer store" }
    };

    protected static ArtifactFact Artifact(ArtifactKey key, string id, string name) => new()
    {
        Id = Id(id),
        Subject = key.Subject,
        Evidence = Evidence(),
        Definition = new ArtifactDefinition { Key = key, Name = name, Properties = [new PropertyDefinition { Name = "name", Type = new TypeReferenceDefinition { Name = "String" } }] }
    };

    protected static ArtifactPlacementFact Placement(ArtifactKey key, string id, string slice = "Register") => new()
    {
        Id = Id(id),
        Subject = key.Subject,
        Evidence = Evidence(),
        Artifact = key,
        Placement = new ArtifactPlacement { Module = "Customers", Slice = slice, SliceKind = key.Kind == ArtifactKind.ReadModel ? GenerationSliceKind.StateView : GenerationSliceKind.StateChange }
    };

    protected static RelationshipFact Reads(ArtifactKey source, ArtifactKey target, string id) => new()
    {
        Id = Id(id),
        Subject = source.Subject,
        Evidence = Evidence(),
        Definition = new RelationshipDefinition
        {
            Key = new RelationshipKey { Kind = RelationshipKind.Reads, Source = source.Subject, Target = target.Subject, SourceScope = source.Scope, TargetScope = target.Scope }
        }
    };

    protected static AdapterContribution Contribution(params GenerationFact[] facts) => new() { Adapter = Adapter, Facts = facts };

    protected static GeneratedScreenplayDefinition Generate(params GenerationFact[] facts) =>
        new ScreenplayDefinitionGenerator().Generate([Contribution(facts)], new ScreenplayGenerationOptions { Domain = "Banking" });

    protected static AdapterContributionAdmissionResult Admit(params GenerationFact[] facts)
    {
        var descriptor = new AdapterDescriptor
        {
            Identity = Adapter,
            SourceLanguage = AdapterSourceLanguage.SourceIndependent,
            Category = AdapterCategory.ApplicationFramework,
            EmittedFactCapabilities = [.. Enum.GetValues<GenerationFactCapability>().Where(capability => capability != GenerationFactCapability.Unknown)]
        };
        return AdapterContributionAdmission.Admit(descriptor, Contribution(facts), new FixtureSourceAuthority());
    }

    protected static GeneratedScreenplayDefinition GenerateSnapshot(params GenerationFact[] facts)
    {
        var admitted = Admit(facts);
        admitted.Diagnostics.ShouldBeEmpty();
        var descriptor = admitted.Snapshot.Descriptor;
        var record = new AdapterRunRecord
        {
            Considered = true,
            Probed = true,
            Executed = true,
            Descriptor = descriptor,
            Probe = new AdapterProbeApplicable(),
            Execution = new AdapterExecutionCompleted { Contribution = admitted.Snapshot },
            Disposition = AdapterRunDisposition.Admitted
        };
        var snapshot = new AdapterRunSnapshot
        {
            Adapters = [record],
            Facts = [.. facts.Select(fact => new GenerationFactRecord { Fact = fact })]
        };
        return new ScreenplayDefinitionGenerator().Generate(snapshot, new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    sealed class FixtureSourceAuthority : ISourceAuthorityValidator
    {
        public bool IsAuthoritative(SourceRange source) =>
            source.Path == "Stores/Registration.cs" &&
            source.StartLine == 3 && source.EndLine == 3 &&
            source.StartColumn == 1 && source.EndColumn == 40;
    }
}
