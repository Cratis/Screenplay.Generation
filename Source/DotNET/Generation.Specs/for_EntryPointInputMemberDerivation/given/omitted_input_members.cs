// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation.given;

public class omitted_input_members : input_members
{
    protected static readonly SubjectId FlagSubject = new() { Value = "dotnet://Library/Permissions" };

    protected static DeclarationInputMemberTypeUseFact FlagType(params TypeUseShapeKind[] shape) => SurfaceType(type: new TypeUseDefinition
    {
        Name = "Int", ObservedTypeSubject = FlagSubject, Shape = shape.Length == 0 ? [TypeUseShapeKind.Named] : shape
    });

    protected static DeclarationInputMemberTargetOmittedFact Omission() => new()
    {
        Id = new FactId { Value = "library:input-target-omitted" }, Subject = Declaration, Evidence = FlagType().Evidence,
        Definition = new DeclarationInputMemberTargetOmittedDefinition
        {
            Member = Member(),
            Diagnostic = new GenerationDiagnostic
            {
                Code = "library.FlagsOmitted", Severity = GenerationDiagnosticSeverity.Warning,
                Message = "No target is contributed", Subject = Declaration, Source = FlagType().Evidence.Source, Facts = [FlagType().Id]
            }
        }
    };

    protected static GenerationFact[] OmittedFacts(params TypeUseShapeKind[] shape) =>
        [.. InputFacts().Where(fact => fact is not DeclarationInputMemberTypeUseFact), FlagType(shape), Omission()];

    protected static GeneratedScreenplayDefinition Generate(IEnumerable<GenerationFact> facts) => new ScreenplayDefinitionGenerator().Generate(
        Run([.. facts, Placement(Transfer().Definition.Artifact), Placement(Document), Returns()]), new ScreenplayGenerationOptions { Domain = "Application" });

    static RelationshipFact Returns() => new()
    {
        Id = new FactId { Value = "query-returns-document" }, Subject = Transfer().Definition.Artifact.Subject, Evidence = UsageEvidence(ApplicationAdapter),
        Definition = new RelationshipDefinition { Key = new RelationshipKey { Kind = RelationshipKind.Returns, Source = Transfer().Definition.Artifact.Subject, Target = Document.Subject } }
    };

    static ArtifactPlacementFact Placement(ArtifactKey artifact) => new()
    {
        Id = new FactId { Value = $"placement:{artifact.Subject.Value}" }, Subject = artifact.Subject, Evidence = UsageEvidence(ApplicationAdapter), Artifact = artifact,
        Placement = new ArtifactPlacement { Module = "Customers", Features = ["Listing"], Slice = "FindCustomer", SliceKind = GenerationSliceKind.StateView }
    };
}
