// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class DeclarationInputMemberTargetOmissions
{
    public static void Validate(GenerationFact fact, IReadOnlyList<GenerationFact> facts, List<GenerationDiagnostic> diagnostics)
    {
        if (fact is not DeclarationInputMemberTargetOmittedFact omission)
        {
            return;
        }

        var typeUses = facts.OfType<DeclarationInputMemberTypeUseFact>().Where(candidate => candidate.Definition.Member == omission.Definition.Member).ToArray();
        if (Valid(omission) && typeUses.Length > 0 && typeUses.All(candidate => Matches(candidate, omission)))
        {
            return;
        }

        diagnostics.Add(new()
        {
            Code = GenerationDiagnosticCodes.InvalidDeclarationInputMemberTargetOmission,
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = GenerationDiagnosticOutcome.Unknown,
            Message = "Declaration input target omission requires an exact same-producer type use and a producer-owned warning",
            Subject = omission.Subject,
            Source = omission.Evidence.Source,
            Facts = [omission.Id]
        });
    }

    public static DeclarationInputMemberTargetOmittedFact[] For(IEnumerable<GenerationFact> inputs)
    {
        var facts = inputs.ToArray();
        var typeUses = facts.OfType<DeclarationInputMemberTypeUseFact>().ToArray();
        return [.. facts.OfType<DeclarationInputMemberTargetOmittedFact>().Where(omission =>
            Valid(omission) && typeUses.Any(candidate => Matches(candidate, omission)))];
    }

    public static TypeUseTargetOmittedFact Transfer(DeclarationInputMemberTargetOmittedFact omission, ArtifactMemberTypeUseFact typeUse, FactId id) => new()
    {
        Id = id,
        Subject = typeUse.Subject,
        Evidence = typeUse.Evidence,
        Definition = new TypeUseTargetOmittedDefinition
        {
            Member = typeUse.Definition.Member,
            Diagnostic = omission.Definition.Diagnostic with { Subject = typeUse.Subject, Source = typeUse.Evidence.Source }
        }
    };

    static bool Matches(DeclarationInputMemberTypeUseFact typeUse, DeclarationInputMemberTargetOmittedFact omission) =>
        typeUse.Definition.Member == omission.Definition.Member && typeUse.Subject == omission.Subject &&
        typeUse.Evidence.Adapter == omission.Evidence.Adapter && typeUse.Definition.Type.ObservedTypeSubject is not null;

    static bool Valid(DeclarationInputMemberTargetOmittedFact omission) =>
        omission.Subject == omission.Definition.Member.Declaration && Warning(omission.Definition.Diagnostic) &&
        omission.Definition.Diagnostic.Subject == omission.Subject && omission.Definition.Diagnostic.Source == omission.Evidence.Source;

    static bool Warning(GenerationDiagnostic diagnostic) => diagnostic.Severity == GenerationDiagnosticSeverity.Warning &&
        !string.IsNullOrWhiteSpace(diagnostic.Code) && !string.IsNullOrWhiteSpace(diagnostic.Message);
}
