// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_using_alternative_vogen_declaration_api_subsets : given.a_vogen_compilation
{
    AdapterRunSnapshot _genericModern = null!;
    AdapterRunSnapshot _genericLegacy = null!;
    AdapterRunSnapshot _nonGenericModern = null!;
    AdapterRunSnapshot _nonGenericLegacy = null!;
    AdapterProbeApplicable _genericProbe = null!;
    AdapterProbeApplicable _nonGenericProbe = null!;
    string[] _compilationErrors = null!;

    void Because()
    {
        var genericCompilation = CompilationFromVogenApiSubset(
            "GenericConcepts",
            """
            namespace Vogen;
            [System.AttributeUsage(System.AttributeTargets.Struct | System.AttributeTargets.Class)]
            public sealed class ValueObjectAttribute<T> : System.Attribute { }
            public readonly struct Validation
            {
                public static Validation Ok => default;
                public static Validation Invalid(string message) => default;
            }
            """,
            new SourceFile(
                "/checkout/Generic/CustomerCode.cs",
                """
                namespace Concepts;
                [Vogen.ValueObject<string>]
                public partial struct CustomerCode
                {
                    private static Vogen.Validation Validate(string value) => Vogen.Validation.Invalid("Required");
                }
                """));
        var nonGenericCompilation = CompilationFromVogenApiSubset(
            "NonGenericConcepts",
            """
            namespace Vogen;
            [System.AttributeUsage(System.AttributeTargets.Struct | System.AttributeTargets.Class)]
            public sealed class ValueObjectAttribute : System.Attribute
            {
                public ValueObjectAttribute(System.Type type) { }
            }
            """,
            new SourceFile(
                "/checkout/NonGeneric/CustomerNumber.cs",
                """
                namespace Concepts;
                [Vogen.ValueObject(typeof(int))]
                public partial struct CustomerNumber { }
                """));
        _compilationErrors =
        [
            .. genericCompilation.GetDiagnostics()
                .Concat(nonGenericCompilation.GetDiagnostics())
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.Id)
        ];

        var genericContext = new DotNetAnalysisContext(
            [MappedProject("Generic.Project", "generic-project", genericCompilation)]);
        var nonGenericContext = new DotNetAnalysisContext(
            [MappedProject("NonGeneric.Project", "non-generic-project", nonGenericCompilation)]);
        _genericProbe = (AdapterProbeApplicable)new VogenConceptScreenplayAdapter().Probe(genericContext);
        _nonGenericProbe = (AdapterProbeApplicable)new VogenConceptScreenplayAdapter().Probe(nonGenericContext);
        (_genericModern, _genericLegacy) = RunBoth(genericContext);
        (_nonGenericModern, _nonGenericLegacy) = RunBoth(nonGenericContext);
    }

    [Fact] void should_compile_the_realistic_api_subsets() => _compilationErrors.ShouldBeEmpty();
    [Fact] void should_execute_with_only_the_generic_declaration_api() => Disposition(_genericModern).ShouldEqual(AdapterRunDisposition.Admitted);
    [Fact] void should_execute_with_only_the_non_generic_declaration_api() => Disposition(_nonGenericModern).ShouldEqual(AdapterRunDisposition.Admitted);
    [Fact] void should_prove_the_same_declaration_capability_from_either_attribute_shape() => new[] { _genericProbe, _nonGenericProbe }.All(probe => Capabilities(probe).Contains("vogen.value-object-declaration", StringComparer.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_require_validation_for_non_generic_concept_applicability() => Capabilities(_nonGenericProbe).ShouldContainOnly("vogen.value-object-declaration");
    [Fact] void should_capture_validation_result_evidence_when_that_optional_api_is_present() => Capabilities(_genericProbe).ShouldContain("vogen.validation-result");
    [Fact] void should_extract_the_validation_message_when_the_optional_api_is_present() => ValidationMessage(_genericModern).ShouldEqual("Required");
    [Fact] void should_keep_generic_modern_and_legacy_contributions_identical() => Facts(_genericModern).ShouldEqual(Facts(_genericLegacy));
    [Fact] void should_keep_non_generic_modern_and_legacy_contributions_identical() => Facts(_nonGenericModern).ShouldEqual(Facts(_nonGenericLegacy));

    static (AdapterRunSnapshot Modern, AdapterRunSnapshot Legacy) RunBoth(DotNetAnalysisContext context)
    {
        var modern = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(new VogenConceptScreenplayAdapter())],
            context,
            new DotNetAdapterOptions());
        var legacy = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.ForLegacy(new VogenConceptScreenplayAdapter())],
            context,
            new DotNetAdapterOptions());
        return (modern, legacy);
    }

    static AdapterRunDisposition Disposition(AdapterRunSnapshot snapshot) => snapshot.Adapters.Single().Disposition;

    static string[] Capabilities(AdapterProbeResult probe) =>
        [
            .. probe.Evidence
                .Where(evidence => evidence.ApiCapability is not null)
                .Select(evidence => evidence.ApiCapability!.Id)
        ];

    static string? ValidationMessage(AdapterRunSnapshot snapshot) =>
        snapshot.Facts
            .Select(record => record.Fact)
            .OfType<ConceptValidationRuleFact>()
            .Single()
            .Definition.Message;

    static string Facts(AdapterRunSnapshot snapshot) => string.Join(
        '|',
        snapshot.Facts.Select(record => record.Fact switch
        {
            ConceptValidationRuleFact validation => $"{validation.GetType().Name}:{validation.Id.Value}:{validation.Subject.Value}:{validation.Definition.Message}",
            var fact => $"{fact.GetType().Name}:{fact.Id.Value}:{fact.Subject.Value}"
        }));
}
