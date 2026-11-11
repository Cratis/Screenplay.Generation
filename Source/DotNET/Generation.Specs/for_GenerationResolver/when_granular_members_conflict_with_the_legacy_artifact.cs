// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_granular_members_conflict_with_the_legacy_artifact : given.granular_artifact_facts
{
    ResolvedApplicationGraph _graph = null!;

    void Because() => _graph = Resolve([
        LegacyCommand(
            "legacy-command",
            1,
            Property("type", "String"),
            Property("order", "String"),
            Property("collection", "String"),
            Property("subject", "String", subject: SecondEventSubject, kind: ArtifactKind.Event)),
        Declaration("decl-command", 2),
        Declaration("decl-first", 3, FirstEvent, "OrderSubmitted"),
        Declaration("decl-second", 4, SecondEvent, "OrderRejected"),
        Order("type-order", 10, "type", 0),
        TypeUse("type-type", 11, "type", "Int32"),
        Order("order-order", 12, "order", 5),
        TypeUse("order-type", 13, "order", "String"),
        Order("collection-order", 14, "collection", 2),
        TypeUse("collection-type", 15, "collection", "String", TypeUseShapeKind.Collection, TypeUseShapeKind.Named),
        Order("subject-order", 16, "subject", 3),
        TypeUse("subject-type", 17, "subject", "String"),
        Binding("subject-binding", 18, "subject", FirstEvent)]);

    [Fact]
    void should_report_each_legacy_conflict() => string.Join('\n', DiagnosticLines(_graph)).ShouldEqual(
        """
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'type' has incompatible legacy and granular type uses. Input facts: 'decl-command', 'legacy-command', 'type-type'|decl-command,legacy-command,type-type|Orders/SubmitOrder.cs:2:3-4:5
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'order' has incompatible declaration orders. Input facts: 'decl-command', 'legacy-command', 'order-order', 'order-type'|decl-command,legacy-command,order-order,order-type|Orders/SubmitOrder.cs:2:3-4:5
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'subject' binds incompatible exact type subjects. Input facts: 'decl-command', 'legacy-command', 'subject-binding', 'subject-type'|decl-command,legacy-command,subject-binding,subject-type|Orders/SubmitOrder.cs:2:3-4:5
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'collection' has incompatible legacy and granular type uses. Input facts: 'collection-type', 'decl-command', 'legacy-command'|collection-type,decl-command,legacy-command|Orders/SubmitOrder.cs:15:16-17:18
        """);

    [Fact]
    void should_omit_the_artifact() => string.Join('\n', ArtifactLines(_graph)).ShouldEqual(
        """
        Event|dotnet://Ordering/Events.OrderRejected|OrderRejected|file=|files=
        Event|dotnet://Ordering/Events.OrderSubmitted|OrderSubmitted|file=|files=
        """);
}
