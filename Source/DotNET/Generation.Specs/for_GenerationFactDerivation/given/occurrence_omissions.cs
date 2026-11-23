// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation.given;

public class occurrence_omissions : diagnostic_projection
{
    protected static readonly SubjectId Document = new() { Value = "symbol://types/Document" };

    protected static OccurrenceOmissionDiagnosticFact Candidate(string producer, OmissionDiagnosticEvidencePreference preference = OmissionDiagnosticEvidencePreference.UseSite)
    {
        var source = Invocation with { Path = $"{producer}.cs", FileIdentity = new() { Project = "project://sample", Path = $"Sources/{producer}.cs" } };
        return new()
        {
            Id = new() { Value = $"{producer}:omission" }, Subject = Document,
            Evidence = new() { Adapter = new() { Id = producer, Version = "1.0.0" }, Strength = EvidenceStrength.Exact, Source = source },
            Definition = new()
            {
                Family = new() { Value = "document-semantics" }, Occurrence = new() { Subject = Document, Kind = ArtifactKind.Document },
                Policy = OccurrenceOmissionDiagnosticPolicy.OnePerOccurrence, Preference = preference,
                Diagnostic = new() { Code = "OMIT0044", Severity = GenerationDiagnosticSeverity.Information, Outcome = GenerationDiagnosticOutcome.Unsupported, Message = "Document semantics cannot be represented", Subject = Document, Source = source, Facts = [new() { Value = $"{producer}:observation" }] }
            }
        };
    }

    protected static DeclarationInputMemberDeclarationFact OmissionObservation(OccurrenceOmissionDiagnosticFact candidate) => new()
    {
        Id = new() { Value = $"{candidate.Evidence.Adapter.Id}:observation" }, Subject = candidate.Subject, Evidence = candidate.Evidence,
        Definition = new() { Member = new() { Declaration = candidate.Subject, Scope = candidate.Definition?.Occurrence?.Scope, Name = "Id" }, DeclarationOrder = 0 }
    };

    protected static GenerationFact[] CandidateFacts(params OccurrenceOmissionDiagnosticFact[] candidates) => [.. candidates.SelectMany(candidate => new GenerationFact[] { candidate, OmissionObservation(candidate) })];

    protected static GenerationDiagnostic[] Omissions(GenerationDerivationSnapshot result) => [.. result.Rules.Where(rule => rule.Rule.Id == "cratis.screenplay.occurrence-omission-diagnostic").SelectMany(rule => rule.Diagnostics)];

    protected static MethodFactSupportFact Conditional(OccurrenceOmissionDiagnosticFact candidate) => new()
    {
        Id = new() { Value = $"{candidate.Evidence.Adapter.Id}:conditional" }, Subject = candidate.Subject, Evidence = candidate.Evidence,
        Definition = new() { Candidate = candidate.Id, Alternatives = [new() { Method = Method, Scope = candidate.Definition.Occurrence.Scope }] }
    };

    protected static BackendScopeFact ScopeFact(BackendScopeKey scope) => new()
    {
        Id = new() { Value = $"application:scope:{scope.EventModelName}" }, Subject = scope.Store,
        Evidence = new() { Adapter = ApplicationAdapter, Strength = EvidenceStrength.Exact, Source = Invocation }, Definition = new() { Key = scope, Name = scope.EventModelName ?? "Store" }
    };

    protected static AdapterContribution[] Contributions(IEnumerable<GenerationFact> facts) => [.. facts.GroupBy(fact => fact.Evidence.Adapter).Select(group => new AdapterContribution { Adapter = group.Key, Facts = [.. group] })];

    protected static AdapterRunSnapshot OmissionSnapshot(IEnumerable<GenerationFact> facts)
    {
        var contributions = Contributions(facts);
        return new()
        {
            Facts = [.. contributions.SelectMany(contribution => contribution.Facts).Select(fact => new GenerationFactRecord { Fact = fact })],
            Adapters = [.. contributions.Select(Completed)]
        };
    }

    static AdapterRunRecord Completed(AdapterContribution contribution)
    {
        var descriptor = new AdapterDescriptor { Identity = contribution.Adapter, SourceLanguage = AdapterSourceLanguage.SourceIndependent, Category = AdapterCategory.ApplicationFramework };
        return new()
        {
            Descriptor = descriptor, Probe = new AdapterProbeApplicable(), Disposition = AdapterRunDisposition.Admitted,
            Execution = new AdapterExecutionCompleted { Contribution = new() { Descriptor = descriptor, Facts = [.. contribution.Facts], Diagnostics = [.. contribution.Diagnostics] } }
        };
    }
}
