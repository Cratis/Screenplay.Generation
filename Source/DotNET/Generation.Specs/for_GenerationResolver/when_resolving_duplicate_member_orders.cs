// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_duplicate_member_orders : given.granular_artifact_facts
{
    ResolvedApplicationGraph _graph = null!;

    void Because() => _graph = Resolve([
        LegacyCommand("legacy-command", 1, Property("existing", "String")),
        Declaration("decl-command", 2),
        .. ValidMember("kilo", 1, 10),
        .. ValidMember("lima", 1, 20),
        .. ValidMember("mike", 2, 30),
        .. ValidMember("november", 2, 40),
        .. ValidMember("oscar", 0, 50)]);

    [Fact]
    void should_report_each_duplicated_order_ascending() => string.Join('\n', DiagnosticLines(_graph)).ShouldEqual(
        """
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact 'dotnet://Ordering/Commands.SubmitOrder' has multiple members at declaration order 0. Input facts: 'legacy-command', 'oscar-order', 'oscar-type'|legacy-command,oscar-order,oscar-type|Orders/SubmitOrder.cs:1:2-3:4
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact 'dotnet://Ordering/Commands.SubmitOrder' has multiple members at declaration order 2. Input facts: 'legacy-command', 'mike-order', 'mike-type', 'november-order', 'november-type'|legacy-command,mike-order,mike-type,november-order,november-type|Orders/SubmitOrder.cs:1:2-3:4
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact 'dotnet://Ordering/Commands.SubmitOrder' has multiple members at declaration order 1. Input facts: 'kilo-order', 'kilo-type', 'legacy-command', 'lima-order', 'lima-type'|kilo-order,kilo-type,legacy-command,lima-order,lima-type|Orders/SubmitOrder.cs:10:11-12:13
        """);

    [Fact]
    void should_omit_the_artifact() => string.Join('\n', ArtifactLines(_graph)).ShouldEqual(string.Empty);
}
