// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_using_the_modern_descriptor_and_probe : given.a_vogen_compilation
{
    VogenConceptScreenplayAdapter _adapter = null!;
    AdapterProbeApplicable _probe = null!;
    AdapterRunRecord _record = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            "Concepts",
            new SourceFile(
                "/checkout/Concepts/CustomerId.cs",
                """
                namespace Concepts;
                [Vogen.ValueObject<System.Guid>]
                public partial struct CustomerId
                {
                    private static Vogen.Validation Validate(System.Guid value) => Vogen.Validation.Ok;
                }
                """));
        var context = new DotNetAnalysisContext([MappedProject("Concepts.Project", "concepts-project", compilation)]);
        _adapter = new VogenConceptScreenplayAdapter();
        _probe = (AdapterProbeApplicable)_adapter.Probe(context);
        _record = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(_adapter)],
            context,
            new DotNetAdapterOptions()).Adapters.Single();
    }

    [Fact] void should_describe_the_exact_identity_category_and_language() => $"{_adapter.Descriptor.Identity.Id}:{_adapter.Descriptor.Identity.Version}:{_adapter.Descriptor.Category}:{_adapter.Descriptor.SourceLanguage}".ShouldEqual("vogen:1.0.0:Concepts:CSharp");
    [Fact] void should_require_authored_stable_semantic_host_capabilities() => string.Join(',', _adapter.Descriptor.RequiredHostCapabilities).ShouldEqual("AuthoredSource,StableSourceLocations,SemanticAnalysis");
    [Fact] void should_declare_exact_emitted_fact_capabilities() => string.Join(',', _adapter.Descriptor.EmittedFactCapabilities).ShouldEqual("Artifact,ConceptRepresentation,ConceptValidationRule");
    [Fact] void should_require_only_the_alternative_vogen_declaration_capability() => _adapter.Descriptor.RequiredApiCapabilities.Select(capability => capability.Id).ShouldEqual(["vogen.value-object-declaration"]);
    [Fact] void should_prove_the_required_declaration_capability() => _probe.Evidence.Where(evidence => evidence.ApiCapability is not null).Select(evidence => evidence.ApiCapability!.Id).ShouldContain("vogen.value-object-declaration");
    [Fact] void should_capture_optional_validation_result_api_evidence() => _probe.Evidence.Where(evidence => evidence.ApiCapability is not null).Select(evidence => evidence.ApiCapability!.Id).ShouldContain("vogen.validation-result");
    [Fact] void should_anchor_the_authored_declaration_probe_evidence_to_stable_source() => _probe.Evidence.Single(evidence => evidence.Source is not null).Source!.FileIdentity.ShouldEqual(new SourceFileIdentity { Project = "concepts-project", Path = "CustomerId.cs" });
    [Fact] void should_admit_the_modern_vogen_contribution() => _record.Disposition.ShouldEqual(AdapterRunDisposition.Admitted);
}
