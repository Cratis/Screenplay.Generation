// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class a_lowering_fixture : a_generator
{
    protected static SubjectId SubjectOf(string name) => new() { Value = $"dotnet://Banking/{name}" };

    protected static SourceRange Range(string path, int line) => new()
    {
        Path = path,
        StartLine = line,
        StartColumn = 1,
        EndLine = line,
        EndColumn = 20
    };

    protected Evidence EvidenceAt(string path, int line) => new()
    {
        Adapter = Adapter,
        Strength = EvidenceStrength.Exact,
        Source = Range(path, line)
    };

    protected GenerationFact[] Artifact(
        ArtifactKind kind,
        string name,
        string slice,
        GenerationSliceKind sliceKind,
        int line,
        string? file = null,
        string? description = null,
        params PropertyDefinition[] properties)
    {
        var subject = SubjectOf(name);
        var key = new ArtifactKey { Subject = subject, Kind = kind };
        var evidence = EvidenceAt($"Accounts/{slice}/{name}.cs", line);
        return
        [
            new ArtifactFact
            {
                Id = new FactId { Value = $"artifact:{name}" },
                Subject = subject,
                Definition = new ArtifactDefinition
                {
                    Key = key,
                    Name = name,
                    File = file,
                    Description = description,
                    Properties = properties
                },
                Evidence = evidence
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = $"placement:{name}" },
                Subject = subject,
                Artifact = key,
                Placement = new ArtifactPlacement
                {
                    Module = "Accounts",
                    Features = ["Opening"],
                    Slice = slice,
                    SliceKind = sliceKind
                },
                Evidence = evidence
            }
        ];
    }

    protected GenerationFact Relation(RelationshipKind kind, string source, string target, string? discriminator = null, string? sourceMember = null) => new RelationshipFact
    {
        Id = new FactId { Value = $"relationship:{kind}:{source}:{target}" },
        Subject = SubjectOf(source),
        Definition = new RelationshipDefinition
        {
            Key = new RelationshipKey
            {
                Kind = kind,
                Source = SubjectOf(source),
                Target = SubjectOf(target),
                Discriminator = discriminator
            },
            SourceMember = sourceMember
        },
        Evidence = EvidenceAt($"Accounts/Relationships/{source}.cs", 3)
    };

    protected static string Describe(IEnumerable<GenerationDiagnostic> diagnostics) =>
        string.Join('\n', diagnostics.Select(_ => string.Join(
            " | ",
            _.Code,
            _.Severity,
            _.Outcome?.ToString() ?? "-",
            _.Subject?.Value ?? "-",
            _.Source is null
                ? "-"
                : string.Create(CultureInfo.InvariantCulture, $"{_.Source.Path}:{_.Source.StartLine}:{_.Source.StartColumn}-{_.Source.EndLine}:{_.Source.EndColumn}"),
            _.Message)));
}
