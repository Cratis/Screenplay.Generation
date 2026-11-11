// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_one_member_has_simultaneous_errors : given.granular_artifact_facts
{
    static readonly ArtifactKey UndeclaredEvent = new()
    {
        Subject = new SubjectId { Value = "dotnet://Ordering/Events.OrderUndeclared" },
        Kind = ArtifactKind.Event
    };

    ResolvedApplicationGraph _graph = null!;

    void Because() => _graph = Resolve([
        Declaration("decl-command", 1),
        Declaration("decl-first", 2, FirstEvent, "OrderSubmitted"),
        Declaration("decl-second", 3, SecondEvent, "OrderRejected"),
        Order("alpha-order-1", 10, "alpha", 1),
        Order("alpha-order-2", 11, "alpha", 2),
        TypeUse("alpha-type-1", 12, "alpha", "String"),
        TypeUse("alpha-type-2", 13, "alpha", "Int32"),
        Binding("alpha-binding-1", 14, "alpha", FirstEvent),
        Binding("alpha-binding-2", 15, "alpha", SecondEvent),
        Role("alpha-role-1", 16, "alpha", ArtifactMemberRoleKind.Identifier),
        Role("alpha-role-2", 17, "alpha", ArtifactMemberRoleKind.EventSourceIdentifier),
        Order("bravo-order", 20, "bravo", 3),
        TypeUse("bravo-type-1", 21, "bravo", "String"),
        TypeUse("bravo-type-2", 22, "bravo", "Int32"),
        Binding("bravo-binding-1", 23, "bravo", FirstEvent),
        Binding("bravo-binding-2", 24, "bravo", SecondEvent),
        Role("bravo-role-1", 25, "bravo", ArtifactMemberRoleKind.Identifier),
        Role("bravo-role-2", 26, "bravo", ArtifactMemberRoleKind.EventSourceIdentifier),
        Order("charlie-order", 30, "charlie", 4),
        TypeUse("charlie-type", 31, "charlie", "String"),
        Binding("charlie-binding-1", 32, "charlie", FirstEvent),
        Binding("charlie-binding-2", 33, "charlie", SecondEvent),
        Role("charlie-role-1", 34, "charlie", ArtifactMemberRoleKind.Identifier),
        Role("charlie-role-2", 35, "charlie", ArtifactMemberRoleKind.EventSourceIdentifier),
        Order("delta-order", 40, "delta", 5),
        TypeUse("delta-type", 41, "delta", "String"),
        Binding("delta-binding", 42, "delta", FirstEvent),
        Role("delta-role-1", 43, "delta", ArtifactMemberRoleKind.Identifier),
        Role("delta-role-2", 44, "delta", ArtifactMemberRoleKind.EventSourceIdentifier),
        Order("echo-order", 50, "echo", 6),
        TypeUse("echo-type", 51, "echo", "String"),
        Binding("echo-binding", 52, "echo", UndeclaredEvent),
        Role("echo-role-1", 53, "echo", ArtifactMemberRoleKind.Identifier),
        Role("echo-role-2", 54, "echo", ArtifactMemberRoleKind.EventSourceIdentifier)]);

    [Fact]
    void should_report_only_the_earliest_error_of_each_member() => string.Join('\n', DiagnosticLines(_graph)).ShouldEqual(
        """
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'alpha' has incompatible declaration orders. Input facts: 'alpha-binding-1', 'alpha-binding-2', 'alpha-order-1', 'alpha-order-2', 'alpha-role-1', 'alpha-role-2', 'alpha-type-1', 'alpha-type-2', 'decl-command'|alpha-binding-1,alpha-binding-2,alpha-order-1,alpha-order-2,alpha-role-1,alpha-role-2,alpha-type-1,alpha-type-2,decl-command|Orders/SubmitOrder.cs:14:15-16:17
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'bravo' has incompatible exact type uses. Input facts: 'bravo-type-1', 'bravo-type-2'|bravo-type-1,bravo-type-2|Orders/SubmitOrder.cs:21:22-23:24
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'charlie' has incompatible exact type bindings. Input facts: 'charlie-binding-1', 'charlie-binding-2'|charlie-binding-1,charlie-binding-2|Orders/SubmitOrder.cs:32:33-34:35
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'delta' has incompatible identifier roles. Input facts: 'delta-role-1', 'delta-role-2'|delta-role-1,delta-role-2|Orders/SubmitOrder.cs:43:44-45:46
        GEN0055|Error|Unknown|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'echo' binds undeclared target role 'Event' for subject 'dotnet://Ordering/Events.OrderUndeclared'. Input facts: 'echo-binding'|echo-binding|Orders/SubmitOrder.cs:52:53-54:55
        """);

    [Fact]
    void should_omit_the_failed_artifact() => string.Join('\n', ArtifactLines(_graph)).ShouldEqual(
        """
        Event|dotnet://Ordering/Events.OrderRejected|OrderRejected|file=|files=
        Event|dotnet://Ordering/Events.OrderSubmitted|OrderSubmitted|file=|files=
        """);
}
