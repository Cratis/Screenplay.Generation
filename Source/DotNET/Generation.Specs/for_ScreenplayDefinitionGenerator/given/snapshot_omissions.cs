// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class snapshot_omissions : artifact_diagnostic_sources
{
    protected static readonly SubjectId Command = SubjectOf("Commands.DeleteStudent");

    protected GenerationFact[] LegacyDocument(string id, ArtifactKey key, int line = 5) =>
    [
        new ArtifactFact
        {
            Id = new FactId { Value = id },
            Subject = key.Subject,
            Evidence = At(line, 83, 105),
            Definition = new ArtifactDefinition { Key = key, Name = "Student", File = "Student.cs" }
        },
        Placement(key)
    ];

    protected GenerationFact[] DeclaredDocument(string id, ArtifactKey key, int line = 5) =>
    [
        Declaration(id, key, line),
        Placement(key)
    ];

    protected RelationshipFact Deletes(string id, SubjectId target, int line, SubjectId? source = null, RelationshipKind kind = RelationshipKind.Deletes) => new()
    {
        Id = new FactId { Value = id },
        Subject = source ?? Command,
        Evidence = At(line, 10, 30),
        Definition = new RelationshipDefinition
        {
            Key = new RelationshipKey { Kind = kind, Source = source ?? Command, Target = target }
        }
    };

    protected static GenerationDiagnostic[] Of(IEnumerable<GenerationDiagnostic> diagnostics, string code) =>
        [.. diagnostics.Where(diagnostic => diagnostic.Code == code)];

    protected static string Projection(IEnumerable<GenerationDiagnostic> diagnostics) => AdapterRunProjection(diagnostics.ToArray());

    protected static GenerationFactRecord RecordOf(GeneratedScreenplayDefinition result, string id) =>
        result.AdapterRun.Facts.Single(record => record.Fact.Id.Value == id);
}
