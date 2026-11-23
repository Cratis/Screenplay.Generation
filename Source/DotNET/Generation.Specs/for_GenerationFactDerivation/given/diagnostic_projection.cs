// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation.given;

public class diagnostic_projection : a_derivation
{
    protected static readonly SubjectId Method = new() { Value = "symbol://methods/Handle" };
    protected static readonly AdapterIdentity Observer = new() { Id = "observations", Version = "1.0.0" };
    protected static readonly SourceRange Invocation = new() { Path = "Handlers/Handle.cs", StartLine = 4, StartColumn = 3, EndLine = 4, EndColumn = 22 };

    protected static Evidence ObservationEvidence() => new() { Adapter = Observer, Strength = EvidenceStrength.Exact, Source = Invocation };

    protected static ArtifactDeclarationFact Observation() => new()
    {
        Id = new() { Value = "observations:observation" }, Subject = ConceptSubject, Evidence = ObservationEvidence(),
        Definition = new() { Artifact = new() { Subject = ConceptSubject, Kind = ArtifactKind.ReadModel }, Name = "Result" }
    };

    protected static GenerationDiagnostic Payload() => new()
    {
        Code = "OBS0044", Severity = GenerationDiagnosticSeverity.Warning, Outcome = GenerationDiagnosticOutcome.Unsupported,
        Message = "Invocation cannot be represented", Source = Invocation, Subject = Method, Facts = [Observation().Id]
    };

    protected static MethodDiagnosticProjectionFact Intent() => new()
    {
        Id = new() { Value = "observations:projection" }, Subject = Method, Evidence = ObservationEvidence(),
        Definition = new() { Method = Method, EntryPointRole = ArtifactKind.Command, Mode = MethodDiagnosticProjectionMode.EntryPointArtifact, Diagnostic = Payload() }
    };

    protected static MethodFactSupportFact Support() => new()
    {
        Id = new() { Value = "observations:support" }, Subject = Method, Evidence = ObservationEvidence(),
        Definition = new() { Candidate = Intent().Id, Alternatives = [new() { Method = Method }] }
    };

    protected static MethodExecutionClassificationFact Classification(MethodExecutionKind kind = MethodExecutionKind.Ordinary) => new()
    {
        Id = new() { Value = "application:classification" }, Subject = Method,
        Evidence = new() { Adapter = ApplicationAdapter, Strength = EvidenceStrength.Exact },
        Definition = new() { Method = Method, Kind = kind }
    };

    protected static EntryPointFact Entry(string id = "entry", ArtifactKey? artifact = null) => new()
    {
        Id = new() { Value = $"application:{id}" }, Subject = Method,
        Evidence = new() { Adapter = ApplicationAdapter, Strength = EvidenceStrength.Exact },
        Definition = new() { Method = Method, Artifact = artifact ?? Command }
    };

    protected static GenerationFact[] Inputs() => [Observation(), Intent(), Support(), Classification(), Entry(), CommandDeclaration()];

    protected static GenerationDiagnostic Projected(GenerationDerivationSnapshot result) => result.Rules.Single(rule => rule.Rule.Id == "cratis.screenplay.method-diagnostic-projection").Diagnostics.Single();
}
