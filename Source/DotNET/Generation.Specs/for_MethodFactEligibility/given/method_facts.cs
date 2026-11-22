// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility.given;

public class method_facts : for_GenerationFactDerivation.given.an_artifact_usage
{
    protected static readonly SubjectId Parent = new() { Value = "dotnet://Application/Api.Parent()" };
    protected static readonly SubjectId Overload = new() { Value = "dotnet://Application/Api.Find``1(System.Int32)" };

    protected static GenerationDiagnostic Reason() => new()
    {
        Code = "FRONTEND_FAILURE_ONLY",
        Severity = GenerationDiagnosticSeverity.Warning,
        Message = "This failure-only executable is not part of ordinary behavior",
        Source = UsageEvidence().Source,
        Subject = Method
    };

    protected static MethodExecutionClassificationFact Classification(MethodExecutionKind kind, SubjectId? method = null, string id = "classification", BackendScopeKey? scope = null) => new()
    {
        Id = new FactId { Value = $"frontend:{id}" },
        Subject = method ?? Method,
        Evidence = UsageEvidence(new AdapterIdentity { Id = "frontend", Version = "1.0.0" }),
        Definition = new MethodExecutionClassificationDefinition
        {
            Method = method ?? Method,
            Kind = kind,
            Scope = scope,
            OmissionReason = kind == MethodExecutionKind.FailureOnly ? Reason() : null
        }
    };

    protected static MethodFactSupportFact Support(params MethodFactSupportAlternative[] alternatives) => new()
    {
        Id = new FactId { Value = "library:support" },
        Subject = Method,
        Evidence = UsageEvidence(ConceptAdapter),
        Definition = new MethodFactSupportDefinition
        {
            Candidate = Usage().Id,
            Alternatives = alternatives.Length == 0 ? [Alternative()] : alternatives
        }
    };

    protected static MethodFactSupportAlternative Alternative(SubjectId? method = null, SubjectId[]? enclosing = null, BackendScopeKey? scope = null, params GenerationDiagnostic[] diagnostics) => new()
    {
        Method = method ?? Method,
        EnclosingExecutables = enclosing ?? [],
        Scope = scope,
        Diagnostics = diagnostics
    };

    protected static AdapterRunSnapshot Run(GenerationFact[] facts) => new()
    {
        Facts = [.. facts.Select(fact => new GenerationFactRecord { Fact = fact })],
        Adapters = [.. facts.GroupBy(fact => fact.Evidence.Adapter).Select(group => Completed(group.Key, [.. group]))]
    };

    static AdapterRunRecord Completed(AdapterIdentity adapter, GenerationFact[] facts)
    {
        var descriptor = new AdapterDescriptor
        {
            Identity = adapter,
            SourceLanguage = AdapterSourceLanguage.SourceIndependent,
            Category = AdapterCategory.ApplicationFramework,
            EmittedFactCapabilities = [.. Enum.GetValues<GenerationFactCapability>().Where(capability => capability != GenerationFactCapability.Unknown)]
        };
        return new AdapterRunRecord
        {
            Considered = true,
            Probed = true,
            Executed = true,
            Descriptor = descriptor,
            Probe = new AdapterProbeApplicable(),
            Disposition = AdapterRunDisposition.Admitted,
            Execution = new AdapterExecutionCompleted { Contribution = new AdapterContributionSnapshot { Descriptor = descriptor, Facts = [.. facts] } }
        };
    }

    protected static AdapterContribution[] Contributions(params GenerationFact[] controls) =>
    [
        new() { Adapter = ApplicationAdapter, Facts = CompleteFacts().Where(fact => fact.Evidence.Adapter == ApplicationAdapter).ToArray() },
        new() { Adapter = ConceptAdapter, Facts = [.. CompleteFacts().Where(fact => fact.Evidence.Adapter == ConceptAdapter), .. controls.OfType<MethodFactSupportFact>()] },
        new() { Adapter = new AdapterIdentity { Id = "frontend", Version = "1.0.0" }, Facts = [.. controls.OfType<MethodExecutionClassificationFact>()] }
    ];
}
