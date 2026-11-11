// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_overlaying_granular_members_onto_a_legacy_artifact : given.granular_artifact_facts
{
    ResolvedApplicationGraph _graph = null!;

    void Because() => _graph = Resolve([
        LegacyCommand(
            "legacy-command",
            1,
            Property("legacy", "String", identifier: true, subject: FirstEventSubject, kind: ArtifactKind.Event),
            Property("shared", "Int32")),
        Declaration("decl-command", 2),
        Declaration("decl-first", 3, FirstEvent, "OrderSubmitted"),
        Order("shared-order", 10, "shared", 1),
        TypeUse("shared-type", 11, "shared", "Int32"),
        Order("plain-order", 12, "plain", 2),
        TypeUse("plain-type", 13, "plain", "String"),
        Order("optional-order", 14, "optional", 3),
        TypeUse("optional-type", 15, "optional", "String", TypeUseShapeKind.Optional, TypeUseShapeKind.Named),
        Order("collection-order", 16, "collection", 4),
        TypeUse("collection-type", 17, "collection", "String", TypeUseShapeKind.Collection, TypeUseShapeKind.Named),
        Order("both-order", 18, "both", 5),
        TypeUse("both-type", 19, "both", "String", TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named),
        Order("bound-order", 20, "bound", 6),
        TypeUse("bound-type", 21, "bound", "OrderSubmitted", FirstEventSubject),
        Binding("bound-binding", 22, "bound", FirstEvent),
        Order("key-order", 23, "key", 7),
        TypeUse("key-type", 24, "key", "Uuid"),
        Role("key-role", 25, "key", ArtifactMemberRoleKind.EventSourceIdentifier)]);

    [Fact]
    void should_not_report_diagnostics() => DiagnosticLines(_graph).ShouldBeEmpty();

    [Fact]
    void should_overlay_the_members_by_declaration_order_then_name() => string.Join('\n', ArtifactLines(_graph)).ShouldEqual(
        """
        Command|dotnet://Ordering/Commands.SubmitOrder|SubmitOrder|file=|files=
        legacy|String|collection=False|optional=False|subject=dotnet://Ordering/Events.OrderSubmitted|kind=Event|identifier=True
        shared|Int32|collection=False|optional=False|subject=|kind=|identifier=False
        plain|String|collection=False|optional=False|subject=|kind=|identifier=False
        optional|String|collection=False|optional=True|subject=|kind=|identifier=False
        collection|String|collection=True|optional=False|subject=|kind=|identifier=False
        both|String|collection=True|optional=True|subject=|kind=|identifier=False
        bound|OrderSubmitted|collection=False|optional=False|subject=dotnet://Ordering/Events.OrderSubmitted|kind=Event|identifier=False
        key|Uuid|collection=False|optional=False|subject=|kind=|identifier=True
        Event|dotnet://Ordering/Events.OrderSubmitted|OrderSubmitted|file=|files=
        """);
}
