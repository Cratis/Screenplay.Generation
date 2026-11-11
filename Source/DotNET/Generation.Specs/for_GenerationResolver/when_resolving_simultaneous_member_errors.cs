// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_simultaneous_member_errors : given.granular_artifact_facts
{
    ResolvedApplicationGraph _graph = null!;

    void Because() => _graph = Resolve([
        Declaration("decl-command", 1),
        Declaration("decl-first", 2, FirstEvent, "OrderSubmitted"),
        .. ValidMember("aardvark", 11, 200),
        TypeUse("alpha-type", 10, "alpha", "String"),
        Order("bravo-order-1", 20, "bravo", 1),
        Order("bravo-order-2", 21, "bravo", 2),
        Order("charlie-order", 30, "charlie", 3),
        TypeUse("charlie-type-1", 31, "charlie", "String"),
        TypeUse("charlie-type-2", 32, "charlie", "Int32"),
        Order("delta-order", 40, "delta", 4),
        TypeUse("delta-type", 41, "delta", "String", TypeUseShapeKind.Collection, TypeUseShapeKind.Collection, TypeUseShapeKind.Named),
        Order("echo-order", 50, "echo", 5),
        Order("foxtrot-order", 60, "foxtrot", 6),
        TypeUse("foxtrot-type", 61, "foxtrot", "String"),
        Binding("foxtrot-binding-1", 62, "foxtrot", FirstEvent),
        Binding("foxtrot-binding-2", 63, "foxtrot", SecondEvent),
        Order("golf-order", 70, "golf", 7),
        TypeUse("golf-type", 71, "golf", "String", FirstEventSubject),
        Order("hotel-order", 80, "hotel", 8),
        TypeUse("hotel-type", 81, "hotel", "String"),
        Binding("hotel-binding", 82, "hotel", SecondEvent),
        Order("india-order", 90, "india", 9),
        TypeUse("india-type", 91, "india", "String", SecondEventSubject),
        Binding("india-binding", 92, "india", FirstEvent),
        Order("juliet-order", 100, "juliet", 10),
        TypeUse("juliet-type", 101, "juliet", "String"),
        Role("juliet-role-1", 102, "juliet", ArtifactMemberRoleKind.Identifier),
        Role("juliet-role-2", 103, "juliet", ArtifactMemberRoleKind.EventSourceIdentifier)]);

    [Fact]
    void should_report_every_member_error_in_one_pass() => string.Join('\n', DiagnosticLines(_graph)).ShouldEqual(
        """
        GEN0049|Error|Unsupported|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'delta' uses exact shape 'Collection -> Collection -> Named' that Screenplay cannot represent. Input facts: 'delta-type'|delta-type|Orders/SubmitOrder.cs:41:42-43:44
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'india' binds incompatible exact type subjects. Input facts: 'decl-command', 'india-binding', 'india-type'|decl-command,india-binding,india-type|Orders/SubmitOrder.cs:1:2-3:4
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'bravo' has incompatible declaration orders. Input facts: 'bravo-order-1', 'bravo-order-2', 'decl-command'|bravo-order-1,bravo-order-2,decl-command|Orders/SubmitOrder.cs:20:21-22:23
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'charlie' has incompatible exact type uses. Input facts: 'charlie-type-1', 'charlie-type-2'|charlie-type-1,charlie-type-2|Orders/SubmitOrder.cs:31:32-33:34
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'foxtrot' has incompatible exact type bindings. Input facts: 'foxtrot-binding-1', 'foxtrot-binding-2'|foxtrot-binding-1,foxtrot-binding-2|Orders/SubmitOrder.cs:62:63-64:65
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'juliet' has incompatible identifier roles. Input facts: 'juliet-role-1', 'juliet-role-2'|juliet-role-1,juliet-role-2|Orders/SubmitOrder.cs:102:103-104:105
        GEN0051|Error|Unknown|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'golf' has an observed exact type subject but no admitted binding. Input facts: 'decl-command', 'golf-type'|decl-command,golf-type|Orders/SubmitOrder.cs:1:2-3:4
        GEN0051|Error|Unknown|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'alpha' has no declaration order. Input facts: 'alpha-type'|alpha-type|Orders/SubmitOrder.cs:10:11-12:13
        GEN0051|Error|Unknown|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'echo' has no exact type use. Input facts: 'echo-order'|echo-order|Orders/SubmitOrder.cs:50:51-52:53
        GEN0055|Error|Unknown|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'hotel' binds undeclared target role 'Event' for subject 'dotnet://Ordering/Events.OrderRejected'. Input facts: 'hotel-binding'|hotel-binding|Orders/SubmitOrder.cs:82:83-84:85
        """);

    [Fact]
    void should_omit_the_failed_artifact() => string.Join('\n', ArtifactLines(_graph)).ShouldEqual("Event|dotnet://Ordering/Events.OrderSubmitted|OrderSubmitted|file=|files=");
}
