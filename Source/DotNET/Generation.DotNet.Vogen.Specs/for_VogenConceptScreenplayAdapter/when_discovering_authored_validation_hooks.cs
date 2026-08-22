// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_discovering_authored_validation_hooks : given.a_vogen_compilation
{
    AdapterContribution _contribution = null!;
    ConceptValidationRuleFact _customerCodeRule = null!;
    ConceptValidationRuleFact _quantityRule = null!;
    ConceptValidationRuleFact _dynamicCodeRule = null!;
    ConceptValidationRuleFact _ambiguousMessageRule = null!;
    ConceptValidationRuleFact _wrappedMessageRule = null!;
    ConceptValidationRuleFact _localMessageRule = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            "Concepts",
            new SourceFile(
                "/workspace/Concepts/Validation.cs",
                """
                namespace Concepts;
                [Vogen.ValueObject<string>]
                public partial struct CustomerCode
                {
                    private const string InvalidMessage = "Customer codes cannot be blank";
                    private static Vogen.Validation Validate(string value) =>
                        string.IsNullOrWhiteSpace(value) ? Vogen.Validation.Invalid(InvalidMessage) : Vogen.Validation.Ok;
                }
                [Vogen.ValueObject<int>]
                public partial struct Quantity
                {
                    private static Vogen.Validation Validate(int value)
                    {
                        return value > 0 ? Vogen.Validation.Ok : Vogen.Validation.Invalid("Quantity must be positive");
                    }
                }
                [Vogen.ValueObject<string>]
                public partial struct DynamicCode
                {
                    private static Vogen.Validation Validate(string value) => Vogen.Validation.Invalid(MessageFor(value));
                    private static string MessageFor(string value) => $"Invalid: {value}";
                }
                [Vogen.ValueObject<string>]
                public partial struct AmbiguousMessage
                {
                    private static Vogen.Validation Validate(string value) => value.Length == 0
                        ? Vogen.Validation.Invalid("A")
                        : Vogen.Validation.Invalid("B");
                }
                [Vogen.ValueObject<string>]
                public partial struct WrappedMessage
                {
                    private static Vogen.Validation Validate(string value) =>
                        Vogen.Validation.Invalid("Wrapped").WithData("value", value);
                }
                [Vogen.ValueObject<string>]
                public partial struct LocalMessage
                {
                    private static Vogen.Validation Validate(string value)
                    {
                        Vogen.Validation Build() => Vogen.Validation.Invalid("Nested");
                        return Build();
                    }
                }
                """));

        _contribution = Analyze(Project("Concepts.Project", compilation));
        _customerCodeRule = RuleFor("CustomerCode");
        _quantityRule = RuleFor("Quantity");
        _dynamicCodeRule = RuleFor("DynamicCode");
        _ambiguousMessageRule = RuleFor("AmbiguousMessage");
        _wrappedMessageRule = RuleFor("WrappedMessage");
        _localMessageRule = RuleFor("LocalMessage");
    }

    [Fact] void should_emit_one_named_rule_for_each_exact_authored_hook() => _contribution.Facts.OfType<ConceptValidationRuleFact>().Count().ShouldEqual(6);
    [Fact] void should_use_a_stable_rule_identity() => _contribution.Facts.OfType<ConceptValidationRuleFact>().All(_ => _.Definition.RuleIdentity == "vogen.validate").ShouldBeTrue();
    [Fact] void should_point_to_the_authored_predicate() => _contribution.Facts.OfType<ConceptValidationRuleFact>().All(_ => _.Definition.Kind == ConceptValidationRuleKind.NamedPredicate && _.Definition.Predicate == "Validate").ShouldBeTrue();
    [Fact] void should_use_a_stable_fact_identity() => _customerCodeRule.Id.Value.ShouldEqual($"vogen:concept-validation:vogen.validate:{_customerCodeRule.Subject.Value}");
    [Fact] void should_preserve_the_implementation_file() => _contribution.Facts.OfType<ConceptValidationRuleFact>().All(_ => _.Definition.ImplementationFile == "Concepts/Validation.cs").ShouldBeTrue();
    [Fact] void should_anchor_evidence_at_the_authored_method() => _customerCodeRule.Evidence.Source!.StartLine.ShouldEqual(6);
    [Fact] void should_use_exact_evidence() => _contribution.Facts.OfType<ConceptValidationRuleFact>().All(_ => _.Evidence.Strength == EvidenceStrength.Exact).ShouldBeTrue();
    [Fact] void should_preserve_a_semantically_constant_invalid_message_from_an_expression_body() => _customerCodeRule.Definition.Message.ShouldEqual("Customer codes cannot be blank");
    [Fact] void should_preserve_a_semantically_constant_invalid_message_from_a_return_expression() => _quantityRule.Definition.Message.ShouldEqual("Quantity must be positive");
    [Fact] void should_not_infer_a_dynamic_message() => _dynamicCodeRule.Definition.Message.ShouldBeNull();
    [Fact] void should_not_choose_between_multiple_invalid_messages() => _ambiguousMessageRule.Definition.Message.ShouldBeNull();
    [Fact] void should_not_preserve_a_message_from_a_wrapped_invalid_invocation() => _wrappedMessageRule.Definition.Message.ShouldBeNull();
    [Fact] void should_not_preserve_a_message_returned_by_a_local_function() => _localMessageRule.Definition.Message.ShouldBeNull();
    [Fact] void should_not_emit_loss_diagnostics_for_represented_validation() => _contribution.Diagnostics.ShouldBeEmpty();

    ConceptValidationRuleFact RuleFor(string conceptName)
    {
        var concept = ConceptNamed(_contribution, conceptName);
        return _contribution.Facts.OfType<ConceptValidationRuleFact>().Single(_ => _.Subject == concept.Subject);
    }
}
