// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Represents an atomic canonical-input result. Refused and inapplicable requests expose no model.
/// </summary>
public sealed record EsmCanonicalReadResult
{
    internal EsmCanonicalReadResult(EsmAdmissionDecision decision, ExecutableSemanticModel? model = null)
    {
        Decision = decision;
        Model = model;
    }

    /// <summary>Gets the shared policy decision or the typed invalid-input refusal.</summary>
    public EsmAdmissionDecision Decision { get; }

    /// <summary>Gets the admitted model, or null when processing is not admitted.</summary>
    public ExecutableSemanticModel? Model { get; }

    /// <summary>Gets the diagnostics that hosts must display before processing or publication.</summary>
    public ImmutableArray<GenerationDiagnostic> Diagnostics => Decision.Diagnostics;

    /// <summary>Gets whether the result admits a model with no error diagnostics.</summary>
    public bool IsAdmitted => Decision.Disposition == EsmDisposition.Admitted && Model is not null && Diagnostics.All(diagnostic => diagnostic.Severity != GenerationDiagnosticSeverity.Error);

    /// <summary>Gets whether strict validation and admission both succeeded.</summary>
    public bool IsSuccess => IsAdmitted;
}

/// <summary>
/// Strictly validates canonical ESM bytes before applying Generation's explicit admission policy.
/// </summary>
public static class EsmCanonicalReader
{
    /// <summary>
    /// Reads canonical input atomically. Compiler validation alone never implies Generation admission.
    /// </summary>
    /// <param name="bytes">The original canonical ESM bytes.</param>
    /// <param name="surface">The intended processing surface.</param>
    /// <returns>An admitted model or a blocking result with no model.</returns>
    public static EsmCanonicalReadResult Read(ReadOnlySpan<byte> bytes, EsmSurface surface)
    {
        try
        {
            var model = SemanticModelSerializer.Deserialize(bytes);
            var decision = EsmAdmission.Evaluate(model.LanguageVersion, model.SemanticVersion, surface);
            return new(decision, decision.Disposition == EsmDisposition.Admitted ? model : null);
        }
        catch (InvalidSemanticContract exception)
        {
            return new(new EsmAdmissionDecision(
                surface,
                EsmDisposition.Refused,
                EsmAdmissionReason.InvalidCanonicalInput,
                [
                    new GenerationDiagnostic
                    {
                        Code = GenerationDiagnosticCodes.InvalidCanonicalEsm,
                        Severity = GenerationDiagnosticSeverity.Error,
                        Outcome = GenerationDiagnosticOutcome.Unsupported,
                        Message = $"Invalid canonical ESM input for surface '{surface}': {exception.Message}"
                    }
                ]));
        }
    }
}
