// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_hooks_are_not_exact_authored_vogen_hooks : given.a_vogen_compilation
{
    AdapterContribution _contribution = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            "Concepts",
            new SourceFile(
                "/workspace/Concepts/Authored.cs",
                """
                namespace Lookalikes
                {
                    public sealed class Validation;
                    [System.AttributeUsage(System.AttributeTargets.Struct)]
                    public sealed class InstanceAttribute(string name, object value) : System.Attribute;
                }
                namespace Concepts
                {
                    [Vogen.ValueObject<string>]
                    public partial struct WrongName
                    {
                        private static Vogen.Validation Check(string value) => Vogen.Validation.Ok;
                    }
                    [Vogen.ValueObject<string>]
                    public partial struct WrongReturn
                    {
                        private static bool Validate(string value) => true;
                    }
                    [Vogen.ValueObject<string>]
                    public partial struct WrongBacking
                    {
                        private static Vogen.Validation Validate(int value) => Vogen.Validation.Ok;
                    }
                    [Vogen.ValueObject<string>]
                    public partial struct NotStatic
                    {
                        private Vogen.Validation Validate(string value) => Vogen.Validation.Ok;
                    }
                    [Vogen.ValueObject<string>]
                    public partial struct RefInput
                    {
                        private static Vogen.Validation Validate(ref string value) => Vogen.Validation.Ok;
                    }
                    [Vogen.ValueObject<string>]
                    public partial struct GenericHook
                    {
                        private static Vogen.Validation Validate<T>(string value) => Vogen.Validation.Ok;
                    }
                    [Vogen.ValueObject<string>]
                    public partial struct FakeValidation
                    {
                        private static Lookalikes.Validation Validate(string value) => new();
                    }
                    [Lookalikes.Instance("Fake", "value")]
                    [Vogen.ValueObject<string>]
                    public partial struct FakeInstance;
                    [Vogen.ValueObject<string>]
                    public partial struct WrongNormalization
                    {
                        private static int NormalizeInput(string value) => value.Length;
                    }
                    [Vogen.ValueObject<string>]
                    public partial struct GeneratedHooks;
                    [Vogen.ValueObject<string>]
                    public partial struct OtherPartialHooks;
                }
                """),
            new SourceFile(
                "/workspace/Concepts/OtherPartial.cs",
                """
                namespace Concepts;
                public partial struct OtherPartialHooks
                {
                    private static Vogen.Validation Validate(string value) => Vogen.Validation.Invalid("Other partial validation");
                    private static string NormalizeInput(string value) => value.Trim();
                }
                """),
            new SourceFile(
                "/workspace/obj/Concepts/GeneratedHooks.g.cs",
                """
                namespace Concepts;
                [Vogen.Instance("Generated", "value")]
                public partial struct GeneratedHooks
                {
                    private static Vogen.Validation Validate(string value) => Vogen.Validation.Invalid("Generated validation");
                    private static string NormalizeInput(string value) => value.Trim();
                }
                """));
        var authoredSyntaxTrees = compilation.SyntaxTrees
            .Where(_ => _.FilePath != "/workspace/obj/Concepts/GeneratedHooks.g.cs")
            .ToHashSet();

        _contribution = Analyze(Project("Concepts.Project", compilation, authoredSyntaxTrees: authoredSyntaxTrees));
    }

    [Fact] void should_keep_all_proven_vogen_concepts() => _contribution.Facts.OfType<ArtifactFact>().Count().ShouldEqual(11);
    [Fact] void should_not_create_validation_for_wrong_names_or_signatures() => _contribution.Facts.OfType<ConceptValidationRuleFact>().ShouldBeEmpty();
    [Fact] void should_not_create_validation_from_a_fake_return_type() => RulesFor("FakeValidation").ShouldBeEmpty();
    [Fact] void should_not_create_validation_from_generated_only_methods() => RulesFor("GeneratedHooks").ShouldBeEmpty();
    [Fact] void should_not_create_validation_from_another_partial_declaration() => RulesFor("OtherPartialHooks").ShouldBeEmpty();
    [Fact] void should_not_report_wrong_normalization_signatures_as_vogen_behavior() => _contribution.Diagnostics.Select(_ => _.Code).ShouldNotContain(VogenGenerationDiagnosticCodes.InputNormalizationNotRepresented);
    [Fact] void should_ignore_fake_instance_attributes() => _contribution.Diagnostics.Select(_ => _.Code).ShouldNotContain(VogenGenerationDiagnosticCodes.NamedInstanceNotRepresented);
    [Fact] void should_ignore_generated_instance_and_normalization_evidence() => _contribution.Diagnostics.ShouldBeEmpty();

    IEnumerable<ConceptValidationRuleFact> RulesFor(string conceptName)
    {
        var concept = ConceptNamed(_contribution, conceptName);
        return _contribution.Facts.OfType<ConceptValidationRuleFact>().Where(_ => _.Subject == concept.Subject);
    }
}
