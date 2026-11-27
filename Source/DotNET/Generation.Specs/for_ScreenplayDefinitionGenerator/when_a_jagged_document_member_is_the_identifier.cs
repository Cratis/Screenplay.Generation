// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_jagged_document_member_is_the_identifier : given.a_generator
{
    static readonly ArtifactKey _document = new() { Subject = new SubjectId { Value = "dotnet://Ordering/Document.Grid" }, Kind = ArtifactKind.Document };
    static readonly ScreenplayGenerationOptions _options = new() { Domain = "Ordering" };

    GenerationFact[] _native = null!;
    GenerationFact[] _legacy = null!;
    AdapterContributionAdmissionResult _admission = null!;
    GeneratedScreenplayDefinition _oracle = null!;
    GeneratedScreenplayDefinition[] _results = null!;

    void Establish()
    {
        var evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact };
        var jagged = new ArtifactMemberKey { Artifact = _document, Name = "jagged" };
        var serial = new ArtifactMemberKey { Artifact = _document, Name = "serial" };
        var declarationId = new FactId { Value = $"{Adapter.Id}:grid:document" };
        _native =
        [
            new ArtifactDeclarationFact
            {
                Id = declarationId, Subject = _document.Subject, Evidence = evidence,
                Definition = new() { Artifact = _document, Name = "Grid" }
            },
            Member(jagged, 1, evidence),
            Member(serial, 0, evidence),
            TypeUse(jagged, new() { Name = "Int", Shape = [TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Collection, TypeUseShapeKind.Named] }, evidence, true),
            TypeUse(serial, new() { Name = "Int", Shape = [TypeUseShapeKind.Named] }, evidence, false),
            new ArtifactMemberRoleFact
            {
                Id = new FactId { Value = $"{Adapter.Id}:grid:identifier" }, Subject = _document.Subject, Evidence = evidence,
                Definition = new() { Member = jagged, Role = ArtifactMemberRoleKind.EventSourceIdentifier }
            }
        ];
        _legacy =
        [
            new ArtifactFact
            {
                Id = declarationId, Subject = _document.Subject, Evidence = evidence,
                Definition = new()
                {
                    Key = _document, Name = "Grid",
                    Properties =
                    [
                        new() { Name = serial.Name, Type = new() { Name = "Int" } },
                        new() { Name = jagged.Name, IsIdentifier = true, Type = new() { Name = string.Empty, IsCollection = true, IsOptional = true } }
                    ]
                }
            }
        ];
    }

    void Because()
    {
        var completed = Completed(Adapter, _native);
        var descriptor = completed.Descriptor with
        {
            CompatibleGenerationVersions = new GenerationVersionRange { MinimumInclusive = new Version(0, 14, 0) },
            EmittedFactCapabilities = [GenerationFactCapability.ArtifactDeclaration, GenerationFactCapability.ArtifactMemberDeclaration, GenerationFactCapability.ArtifactMemberTypeUse, GenerationFactCapability.ArtifactMemberRole]
        };
        _admission = AdapterContributionAdmission.Admit(descriptor, Contribution(_native));
        var admitted = _admission.Snapshot ?? throw new InvalidOperationException(string.Join("; ", _admission.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var raw = Contribution([.. admitted.Facts]);
        var reversed = raw with { Facts = [.. admitted.Facts.Reverse()] };
        _oracle = Generator.Generate([Contribution(_legacy)], _options);
        _results =
        [
            Generator.Generate(Snapshot(completed with { Descriptor = descriptor, Execution = new AdapterExecutionCompleted { Contribution = admitted } }), _options),
            Generator.Generate(Snapshot(Completed(Adapter, reversed.Facts)), _options),
            Generator.Generate([raw], _options),
            Generator.Generate([reversed], _options)
        ];
    }

    [Fact] void should_admit_the_native_members() => _admission.IsAdmitted.ShouldBeTrue();
    [Fact] void should_match_the_ordered_legacy_properties() => _results.Select(Properties).Distinct().ShouldEqual([Properties(_oracle)]);
    [Fact] void should_match_the_legacy_relationships() => _results.All(result => AdapterRunProjection(result.Graph.Relationships) == AdapterRunProjection(_oracle.Graph.Relationships)).ShouldBeTrue();
    [Fact] void should_match_the_legacy_source_bytes() => _results.All(result => result.Source == _oracle.Source).ShouldBeTrue();
    [Fact] void should_match_the_legacy_verification_status() => _results.All(result => result.IsSuccess == _oracle.IsSuccess).ShouldBeTrue();
    [Fact] void should_match_the_raw_legacy_diagnostics() => _results.Select(result => AdapterRunProjection(result.Diagnostics)).Distinct().ShouldEqual([AdapterRunProjection(_oracle.Diagnostics)]);
    [Fact] void should_preserve_the_existing_document_omission() => _oracle.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual([GenerationDiagnosticCodes.UnsupportedArtifact]);

    static string Properties(GeneratedScreenplayDefinition result) => JsonSerializer.Serialize(result.Graph.Artifacts.Single(artifact => artifact.Key == _document).Variants.Single().Definition.Properties);

    ArtifactMemberDeclarationFact Member(ArtifactMemberKey member, int order, Evidence evidence) => new()
    {
        Id = new FactId { Value = $"{Adapter.Id}:grid:{member.Name}:member" }, Subject = _document.Subject, Evidence = evidence,
        Definition = new() { Member = member, DeclarationOrder = order }
    };

    ArtifactMemberTypeUseFact TypeUse(ArtifactMemberKey member, TypeUseDefinition type, Evidence evidence, bool jagged) => new()
    {
        Id = new FactId { Value = $"{Adapter.Id}:grid:{member.Name}:type-use" }, Subject = _document.Subject, Evidence = evidence,
        Definition = new()
        {
            Member = member, Type = type, UseLegacyJaggedArrayReference = jagged,
            CollectionOrigins = jagged ? [TypeUseCollectionOriginKind.Array, TypeUseCollectionOriginKind.Array] : null
        }
    };
}
