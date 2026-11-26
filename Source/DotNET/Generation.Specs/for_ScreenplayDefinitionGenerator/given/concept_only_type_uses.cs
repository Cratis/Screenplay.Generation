// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class concept_only_type_uses : yielding_concepts
{
    protected GenerationFact[] OptedCommand(params TypeUseShapeKind[] shape)
    {
        var facts = Command(shape);
        return [.. facts, Policy(facts.OfType<ArtifactMemberTypeUseFact>().Single())];
    }

    protected GenerationFact[] ReportingCommand(params TypeUseShapeKind[] shape)
    {
        var facts = Command(shape);
        return [.. facts, Policy(facts.OfType<ArtifactMemberTypeUseFact>().Single(), TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallbackAndMissingConceptDiagnostic)];
    }

    protected static GenerationDiagnostic[] MissingConcept(GeneratedScreenplayDefinition result) =>
        [.. result.Diagnostics.Where(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingConceptReference)];

    protected TypeUseBindingPolicyFact Policy(GenerationFact observation, TypeUseBindingPolicyKind kind = TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallback) => new()
    {
        Id = new() { Value = $"{observation.Id.Value}:concept-only" },
        Subject = observation.Subject,
        Evidence = observation.Evidence,
        Definition = new() { TypeUse = observation.Id, Policy = kind }
    };

    protected ArtifactDeclarationFact NonConcept(ArtifactKind kind = ArtifactKind.ReadModel, BackendScopeKey? scope = null) => new()
    {
        Id = Id($"non-concept-{kind}"),
        Subject = Target,
        Evidence = Command()[0].Evidence,
        Definition = new() { Artifact = new() { Subject = Target, Kind = kind, Scope = scope }, Name = "PermissionsRecord" }
    };

    protected GeneratedScreenplayDefinition GenerateAdapters(GenerationFact[] first, GenerationFact[] second, bool reverse = false)
    {
        var one = Completed(first[0].Evidence.Adapter, first);
        var two = Completed(second[0].Evidence.Adapter, second);
        return Generator.Generate(reverse ? Snapshot(two, one) : Snapshot(one, two), new() { Domain = "Ordering" });
    }

    protected static string Projection(object value) => System.Text.Json.JsonSerializer.Serialize(value);
    protected static bool MissingTarget(GenerationDiagnostic diagnostic) => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget || diagnostic.Code == GenerationDiagnosticCodes.IncompleteArtifactMember || diagnostic.Code == GenerationDiagnosticCodes.MissingConceptReference;
}
