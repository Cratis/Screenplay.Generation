// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_a_failed_legacy_member_shares_an_order : given.granular_artifact_facts
{
    ResolvedApplicationGraph _graph = null!;

    void Because() => _graph = Resolve([
        LegacyCommand("legacy-command", 1, Property("existing", "String")),
        Declaration("decl-command", 2),
        TypeUse("existing-type", 10, "existing", "Int32"),
        .. ValidMember("newcomer", 0, 20)]);

    [Fact]
    void should_keep_both_the_member_error_and_the_duplicate_order_error() => string.Join('\n', DiagnosticLines(_graph)).ShouldEqual(
        """
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact member 'existing' has incompatible legacy and granular type uses. Input facts: 'decl-command', 'existing-type', 'legacy-command'|decl-command,existing-type,legacy-command|Orders/SubmitOrder.cs:2:3-4:5
        GEN0050|Error|Conflict|dotnet://Ordering/Commands.SubmitOrder|Artifact 'dotnet://Ordering/Commands.SubmitOrder' has multiple members at declaration order 0. Input facts: 'existing-type', 'legacy-command', 'newcomer-order', 'newcomer-type'|existing-type,legacy-command,newcomer-order,newcomer-type|Orders/SubmitOrder.cs:10:11-12:13
        """);

    [Fact]
    void should_omit_the_artifact() => string.Join('\n', ArtifactLines(_graph)).ShouldEqual(string.Empty);
}
