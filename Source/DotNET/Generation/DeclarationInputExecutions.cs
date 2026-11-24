// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class DeclarationInputExecutions
{
    public static IEnumerable<Observation> From(IEnumerable<GenerationFact> facts) => facts.Select(Observe).OfType<Observation>();

    public static void Validate(GenerationFact fact, List<GenerationDiagnostic> diagnostics)
    {
        if (fact is MethodDeclarationExecutionFact execution && !Valid(execution))
        {
            diagnostics.Add(new()
            {
                Code = GenerationDiagnosticCodes.InvalidEntryPointInputTransfer,
                Severity = GenerationDiagnosticSeverity.Error,
                Message = "Input execution requires exact method ownership, a declaration identity and nonnegative semantic order",
                Source = fact.Evidence.Source,
                Subject = fact.Subject,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Facts = [fact.Id]
            });
        }
    }

    static bool Valid(MethodDeclarationExecutionFact fact) =>
        fact.Definition is { Method: { } method, Declaration: { } declaration } definition &&
        method == fact.Subject && Normalized(method.Value) && Normalized(declaration.Value) &&
        definition.ExecutionOrder is not < 0;

    static bool Normalized(string value) => !string.IsNullOrWhiteSpace(value) && value == value.Trim();

    static Observation? Observe(GenerationFact fact) => fact switch
    {
        MethodDeclarationExecutionFact execution => new(execution, execution.Definition),
        MethodArtifactUsageFact usage when usage.Definition.Kind == RelationshipKind.Reads => new(usage, new()
        {
            Method = usage.Definition.Method,
            Declaration = usage.Definition.Declaration,
            SourceScope = usage.Definition.SourceScope,
            TargetScope = usage.Definition.TargetScope,
            ExecutionOrder = usage.Definition.ExecutionOrder
        }),
        _ => null
    };

    public sealed record Observation(GenerationFact Fact, MethodDeclarationExecutionDefinition Definition);
}
