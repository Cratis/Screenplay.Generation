// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_slices_to_fixed_source : given.a_lowering_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var accountNumber = Concept("AccountNumber", GenerationPrimitiveKind.Text);
        var ghost = SubjectOf("Concepts.Ghost");
        var ownerType = new TypeReferenceDefinition { Name = "AccountNumber", Subject = accountNumber.Subject };
        var ghostType = new TypeReferenceDefinition { Name = "Ghost", Subject = ghost };
        var identifier = new PropertyDefinition { Name = "accountId", Type = new TypeReferenceDefinition { Name = "Uuid" }, IsIdentifier = true };
        var owner = new PropertyDefinition { Name = "owner", Type = ownerType };
        var tags = new PropertyDefinition { Name = "tags", Type = new TypeReferenceDefinition { Name = "String", IsCollection = true, IsOptional = true } };
        var haunted = new PropertyDefinition { Name = "haunted", Type = ghostType };
        var placedHandler = Artifact(ArtifactKind.Handler, "OpenHandler", "Open", GenerationSliceKind.StateChange, 21);
        var unplacedHandler = Artifact(ArtifactKind.Handler, "StrayHandler", "Open", GenerationSliceKind.StateChange, 22)[0];
        var unplacedCommand = Artifact(ArtifactKind.Command, "StrayCommand", "Open", GenerationSliceKind.StateChange, 23)[0];

        GenerationFact[] facts =
        [
            .. accountNumber.Facts,
            .. Artifact(ArtifactKind.Command, "OpenAccount", "Open", GenerationSliceKind.StateChange, 1, "Accounts\\Open\\OpenAccount.cs", "Opens an account", identifier, owner, tags),
            .. Artifact(ArtifactKind.Command, "CloseAccount", "Open", GenerationSliceKind.StateChange, 2, "Accounts/Open/CloseAccount.cs", null, identifier),
            .. Artifact(ArtifactKind.Command, "ReviewAccount", "Open", GenerationSliceKind.StateChange, 3, null, null, identifier),
            .. Artifact(ArtifactKind.Event, "AccountOpened", "Open", GenerationSliceKind.StateChange, 4, "Accounts/Open/AccountOpened.cs", null, identifier, haunted),
            .. Artifact(ArtifactKind.Event, "AccountClosed", "Open", GenerationSliceKind.StateChange, 5, "Accounts/Open/AccountClosed.cs", null, identifier),
            Relation(RelationshipKind.Produces, "OpenAccount", "AccountOpened"),
            Relation(RelationshipKind.Produces, "OpenAccount", "AccountClosed"),
            Relation(RelationshipKind.Produces, "OpenAccount", "Missing"),
            Relation(RelationshipKind.Reads, "OpenAccount", "AccountView", sourceMember: "accountId"),
            Relation(RelationshipKind.Produces, "CloseAccount", "AccountClosed", "imperative"),
            Relation(RelationshipKind.Produces, "CloseAccount", "AccountOpened"),
            Relation(RelationshipKind.Produces, "ReviewAccount", "AccountOpened"),
            .. Artifact(ArtifactKind.ReadModel, "AccountView", "View", GenerationSliceKind.StateView, 6, "Accounts/View/AccountView.cs", "Account summary", identifier, owner),
            .. Artifact(ArtifactKind.Query, "GetAccount", "View", GenerationSliceKind.StateView, 7, "Accounts/View/GetAccount.cs", "Gets one account", identifier, owner),
            .. Artifact(ArtifactKind.Query, "FindAccounts", "View", GenerationSliceKind.StateView, 8, null, null, owner),
            .. Artifact(ArtifactKind.Query, "Orphan", "View", GenerationSliceKind.StateView, 9),
            .. Artifact(ArtifactKind.Reducer, "AccountReducer", "View", GenerationSliceKind.StateView, 10, "Accounts/View/AccountReducer.cs", "Reduces accounts"),
            .. Artifact(ArtifactKind.Reducer, "BrokenReducer", "View", GenerationSliceKind.StateView, 11),
            .. Artifact(ArtifactKind.Reducer, "EventlessReducer", "View", GenerationSliceKind.StateView, 12),
            Relation(RelationshipKind.Returns, "GetAccount", "AccountView"),
            Relation(RelationshipKind.Returns, "FindAccounts", "AccountView"),
            Relation(RelationshipKind.Builds, "AccountReducer", "AccountView"),
            Relation(RelationshipKind.Consumes, "AccountReducer", "AccountOpened"),
            Relation(RelationshipKind.Consumes, "AccountReducer", "AccountClosed"),
            Relation(RelationshipKind.Consumes, "BrokenReducer", "AccountOpened"),
            Relation(RelationshipKind.Builds, "EventlessReducer", "AccountView"),
            Relation(RelationshipKind.Consumes, "EventlessReducer", "Nowhere"),
            .. Artifact(ArtifactKind.Event, "MixedChange", "Mixed", GenerationSliceKind.StateChange, 13),
            .. Artifact(ArtifactKind.ReadModel, "MixedView", "Mixed", GenerationSliceKind.StateView, 14),
            .. placedHandler,
            unplacedHandler,
            unplacedCommand,
            Relation(RelationshipKind.Unknown, "OpenAccount", "AccountOpened")
        ];

        _result = Generator.Generate([Contribution(facts)], new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact]
    void should_emit_the_complete_fixed_source() => _result.Source.ReplaceLineEndings("\n").ShouldEqual(
        """
        domain Banking

        concept AccountNumber : String
          file Concepts/AccountNumber.cs

        module Accounts

          feature Opening

            slice StateChange Open

              command CloseAccount
                accountId Uuid identifier
                handler
                  file Accounts/Open/CloseAccount.cs

              command OpenAccount
                description "Opens an account"
                accountId Uuid identifier
                owner AccountNumber
                tags String[] optional
                reads AccountView by accountId
                produces AccountClosed
                produces AccountOpened

              command ReviewAccount
                accountId Uuid identifier
                produces AccountOpened

              event AccountClosed
                file Accounts/Open/AccountClosed.cs
                accountId Uuid identifier

            slice StateView View

              query FindAccounts => AccountView
                filter owner AccountNumber

              query GetAccount => AccountView
                description "Gets one account"
                by accountId Uuid
                filter owner AccountNumber
                performer
                  file Accounts/View/GetAccount.cs

              readmodel AccountView
                description "Account summary"
                file Accounts/View/AccountView.cs
                accountId Uuid identifier
                owner AccountNumber

              reducer AccountReducer => AccountView
                description "Reduces accounts"
                on AccountClosed
                  file Accounts/View/AccountReducer.cs
                on AccountOpened
                  file Accounts/View/AccountReducer.cs

        """.ReplaceLineEndings("\n"));

    [Fact]
    void should_report_the_complete_fixed_diagnostics() => Describe(_result.Diagnostics).ShouldEqual(
        """
        GEN0004 | Warning | Unsupported | dotnet://Banking/OpenHandler | Accounts/Open/OpenHandler.cs:21:1-21:20 | The recognized Handler artifact 'OpenHandler' cannot yet be represented by the Screenplay lowerer and was omitted
        GEN0004 | Warning | Unsupported | dotnet://Banking/StrayHandler | Accounts/Open/StrayHandler.cs:22:1-22:20 | The recognized Handler artifact 'StrayHandler' cannot yet be represented by the Screenplay lowerer and was omitted
        GEN0009 | Warning | Unknown | dotnet://Banking/Orphan | - | Query 'Orphan' was omitted because it does not return exactly one known read model
        GEN0009 | Warning | Unknown | dotnet://Banking/BrokenReducer | - | Reducer 'BrokenReducer' was omitted because it does not identify exactly one read model and at least one consumed event
        GEN0009 | Warning | Unknown | dotnet://Banking/EventlessReducer | - | Reducer 'EventlessReducer' was omitted because none of its consumed event subjects resolve to event artifacts
        GEN0009 | Warning | Unknown | dotnet://Banking/StrayCommand | Accounts/Open/StrayCommand.cs:23:1-23:20 | The recognized Command artifact 'StrayCommand' has no Screenplay placement and was omitted
        GEN0016 | Warning | Unknown | dotnet://Banking/Concepts.Ghost | - | Type reference 'Ghost' targets concept 'dotnet://Banking/Concepts.Ghost', which could not be emitted
        GEN0024 | Warning | Unknown | dotnet://Banking/OpenAccount | Accounts/Relationships/OpenAccount.cs:3:1-3:20 | Fact 'relationship:Unknown:OpenAccount:AccountOpened' from adapter 'critter-stack' uses unknown RelationshipKind value '-1'; the affected fact was omitted
        PLAY0166 | Warning | - | - | :24:9-24:9 | Unknown event 'AccountOpened' - declare it with 'event AccountOpened'
        PLAY0166 | Warning | - | - | :28:9-28:9 | Unknown event 'AccountOpened' - declare it with 'event AccountOpened'
        PLAY0166 | Warning | - | - | :56:9-56:9 | Unknown event 'AccountOpened' - declare it with 'event AccountOpened'
        GEN0005 | Error | Unsupported | - | :32:9-32:9 | The generated Screenplay document did not compile: PLAY0019 Property 'accountId' of event 'AccountClosed' cannot be marked identifier - an event never carries its event source id
        GEN0007 | Error | Conflict | dotnet://Banking/MixedChange | Accounts/Mixed/MixedChange.cs:13:1-13:20 | Slice 'Mixed' was assigned incompatible kinds: StateChange, StateView
        """);
}
