// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ArtifactMemberRoleSelectionDiagnosticValidation
{
    public static bool CanTraverse(GenerationDiagnostic? diagnostic) => diagnostic is not null && diagnostic.Lineage is null &&
        !diagnostic.Facts.IsDefault && diagnostic.Facts.All(id => id is not null);

    public static bool Valid(GenerationFact owner, GenerationDiagnostic? diagnostic, IEnumerable<GenerationFact> facts, bool matchingSubject)
    {
        if (diagnostic is null)
        {
            return false;
        }

        return Metadata(diagnostic) && Subject(owner, diagnostic, matchingSubject) && Source(diagnostic.Source) && References(owner, diagnostic, facts);
    }

    static bool Metadata(GenerationDiagnostic diagnostic) => diagnostic.Lineage is null &&
        DiagnosticIntentValidation.Text(diagnostic.Code) && DiagnosticIntentValidation.Text(diagnostic.Message) &&
        diagnostic.Severity != GenerationDiagnosticSeverity.Unknown && Enum.IsDefined(diagnostic.Severity) &&
        (diagnostic.Outcome is null || Enum.IsDefined(diagnostic.Outcome.Value));

    static bool Subject(GenerationFact owner, GenerationDiagnostic diagnostic, bool matchingSubject) =>
        (!matchingSubject || diagnostic.Subject == owner.Subject) &&
        (diagnostic.Subject is null || DiagnosticIntentValidation.Subject(diagnostic.Subject));

    static bool References(GenerationFact owner, GenerationDiagnostic diagnostic, IEnumerable<GenerationFact> facts) =>
        !diagnostic.Facts.IsDefault && diagnostic.Facts.All(id => id is not null && facts.Select(fact => (fact.Id, fact.Evidence.Adapter)).Contains((id, owner.Evidence.Adapter)));

    static bool Source(SourceRange? source) => source is null || (DiagnosticIntentValidation.Text(source.Path) && source.StartLine > 0 && source.StartColumn > 0 &&
        source.EndLine >= source.StartLine && source.EndColumn > 0 && (source.EndLine != source.StartLine || source.EndColumn >= source.StartColumn));
}
