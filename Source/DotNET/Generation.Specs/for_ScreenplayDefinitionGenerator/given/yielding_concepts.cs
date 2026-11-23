// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class yielding_concepts : an_omitted_type_use
{
    protected GenerationFact[] Command(params TypeUseShapeKind[] shape) => [.. Facts(shape).Where(fact => fact is not TypeUseTargetOmittedFact)];

    protected GenerationFact[] Candidate(string suffix, bool yielding, bool primitive = false, AdapterIdentity? producer = null)
    {
        var subject = suffix == "enum" ? Target : new SubjectId { Value = $"dotnet://Ordering/{suffix}" };
        var evidence = new Evidence
        {
            Adapter = producer ?? Adapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange { Path = $"{suffix}.cs", StartLine = 1, StartColumn = 1, EndLine = 1, EndColumn = 30 }
        };
        var id = new FactId { Value = $"{evidence.Adapter.Id}:concept-{suffix}" };
        return
        [
            new ArtifactDeclarationFact
            {
                Id = id, Subject = subject, Evidence = evidence,
                Definition = new ArtifactDeclarationDefinition
                {
                    Artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept }, Name = "Permissions",
                    NameConflictYield = yielding ? new ConceptNameYieldDefinition
                    {
                        Diagnostic = new GenerationDiagnostic
                        {
                            Code = "example.EnumConceptNameConflict", Severity = GenerationDiagnosticSeverity.Warning, Outcome = GenerationDiagnosticOutcome.Conflict,
                            Message = "The enum concept name conflicts with another concept", Subject = subject,
                            Source = evidence.Source, Facts = [id]
                        }
                    } : null
                }
            },
            new ConceptRepresentationFact
            {
                Id = new FactId { Value = $"{evidence.Adapter.Id}:representation-{suffix}" }, Subject = subject, Evidence = evidence,
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = subject,
                    Kind = primitive ? ConceptRepresentationKind.Primitive : ConceptRepresentationKind.Enumeration,
                    Primitive = primitive ? GenerationPrimitiveKind.WholeNumber : null,
                    EnumerationValues = primitive ? [] : ["Read", "Write"]
                }
            }
        ];
    }

    protected GenerationFact[] UnboundMember(string typeName, AdapterIdentity? producer = null)
    {
        var typeUse = Command().OfType<ArtifactMemberTypeUseFact>().Single();
        var member = typeUse.Definition.Member with { Name = "unbound" };
        var evidence = typeUse.Evidence with { Adapter = producer ?? Adapter };
        return
        [
            new ArtifactMemberDeclarationFact
            {
                Id = new FactId { Value = $"{evidence.Adapter.Id}:unbound-declaration" }, Subject = typeUse.Subject, Evidence = evidence,
                Definition = new ArtifactMemberDeclarationDefinition { Member = member, DeclarationOrder = 1 }
            },
            typeUse with
            {
                Id = new FactId { Value = $"{evidence.Adapter.Id}:unbound-type-use" }, Evidence = evidence,
                Definition = new ArtifactMemberTypeUseDefinition { Member = member, Type = new TypeUseDefinition { Name = typeName } }
            }
        ];
    }

    protected static bool IsWarning(GenerationDiagnostic diagnostic) => diagnostic.Code == "example.EnumConceptNameConflict";

    protected static PropertyDefinition Member(GeneratedScreenplayDefinition result) =>
        result.Graph.Artifacts.Single(artifact => artifact.Key.Kind == ArtifactKind.Command).Variants.Single().Definition.Properties.Single();
}
