// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_projected_method_diagnostics : for_GenerationFactDerivation.given.diagnostic_projection
{
    GeneratedScreenplayDefinition _result;
    GenerationDiagnostic _diagnostic;
    void Because()
    {
        _result = new ScreenplayDefinitionGenerator().Generate(new AdapterRunSnapshot { Adapters = [.. Inputs().GroupBy(fact => fact.Evidence.Adapter).Select(Completed)] }, new() { Domain = "Diagnostics" });
        _diagnostic = _result.Diagnostics.Single(diagnostic => diagnostic.Code == Payload().Code);
    }
    static AdapterRunRecord Completed(IGrouping<AdapterIdentity, GenerationFact> facts)
    {
        var descriptor = new AdapterDescriptor { Identity = facts.Key, SourceLanguage = AdapterSourceLanguage.SourceIndependent, Category = AdapterCategory.ApplicationFramework };
        return new()
        {
            Descriptor = descriptor, Disposition = AdapterRunDisposition.Admitted, Probe = new AdapterProbeApplicable(),
            Execution = new AdapterExecutionCompleted { Contribution = new() { Descriptor = descriptor, Facts = [.. facts] } }
        };
    }
    [Fact] void should_emit_the_projected_payload_once() => _diagnostic.Subject.ShouldEqual(CommandSubject);
    [Fact] void should_copy_lineage_through_the_final_run() => JsonSerializer.Serialize(_result.AdapterRun!.Derivation.Diagnostics.Single(diagnostic => diagnostic.Code == Payload().Code)).ShouldEqual(JsonSerializer.Serialize(_diagnostic));
    [Fact] void should_keep_the_intent_as_provenance() => _result.AdapterRun!.Facts.Single(record => record.Fact.Id == Intent().Id).Disposition.ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_not_emit_generic_intent_omission_noise() => _result.Diagnostics.Any(diagnostic => (diagnostic.Code == GenerationDiagnosticCodes.OmittedGenerationFact || diagnostic.Code == GenerationDiagnosticCodes.ConflictingGenerationFact) && diagnostic.Facts.Contains(Intent().Id)).ShouldBeFalse();
}
