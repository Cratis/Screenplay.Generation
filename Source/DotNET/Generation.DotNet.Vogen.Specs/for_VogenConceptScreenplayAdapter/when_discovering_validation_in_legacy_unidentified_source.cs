// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_discovering_validation_in_legacy_unidentified_source : given.a_vogen_compilation
{
    AdapterContribution _contribution = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            "Concepts",
            new SourceFile(
                string.Empty,
                """
                namespace Concepts;

                [Vogen.ValueObject<string>]
                public partial struct CustomerCode
                {
                    private static Vogen.Validation Validate(string value) => Vogen.Validation.Invalid("Customer code is invalid");
                }
                """),
            new SourceFile(
                string.Empty,
                """
                namespace Concepts;
                public static class Unrelated;
                """));

        _contribution = Analyze(Project("Concepts.Project", compilation));
    }

    [Fact] void should_preserve_the_validation_rule_without_cross_tree_identities() => _contribution.Facts.OfType<ConceptValidationRuleFact>().Single().Definition.Message.ShouldEqual("Customer code is invalid");
    [Fact] void should_emit_no_diagnostic_for_legacy_unidentified_source() => _contribution.Diagnostics.ShouldBeEmpty();
}
