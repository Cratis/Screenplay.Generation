// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver.given;

public class granular_artifact_facts : Specification
{
    protected static readonly AdapterIdentity Adapter = new() { Id = "granular", Version = "1.0.0" };
    protected static readonly SubjectId CommandSubject = new() { Value = "dotnet://Ordering/Commands.SubmitOrder" };
    protected static readonly SubjectId FirstEventSubject = new() { Value = "dotnet://Ordering/Events.OrderSubmitted" };
    protected static readonly SubjectId SecondEventSubject = new() { Value = "dotnet://Ordering/Events.OrderRejected" };
    protected static readonly ArtifactKey Command = new() { Subject = CommandSubject, Kind = ArtifactKind.Command };
    protected static readonly ArtifactKey FirstEvent = new() { Subject = FirstEventSubject, Kind = ArtifactKind.Event };
    protected static readonly ArtifactKey SecondEvent = new() { Subject = SecondEventSubject, Kind = ArtifactKind.Event };

    protected static Evidence At(int line) => new()
    {
        Adapter = Adapter,
        Strength = EvidenceStrength.Exact,
        Source = new SourceRange
        {
            Path = "Orders/SubmitOrder.cs",
            StartLine = line,
            StartColumn = line + 1,
            EndLine = line + 2,
            EndColumn = line + 3
        }
    };

    protected static ArtifactFact Legacy(string id, int line, params PropertyDefinition[] properties) => Legacy(id, line, FirstEvent, "SubmitOrder", properties);

    protected static ArtifactFact Legacy(string id, int line, ArtifactKey key, string name, params PropertyDefinition[] properties) => new()
    {
        Id = new FactId { Value = id },
        Subject = key.Subject,
        Evidence = At(line),
        Definition = new ArtifactDefinition { Key = key, Name = name, Properties = properties }
    };

    protected static ArtifactFact LegacyCommand(string id, int line, params PropertyDefinition[] properties) => Legacy(id, line, Command, "SubmitOrder", properties);

    protected static ArtifactDeclarationFact Declaration(string id, int line, ArtifactKey? artifact = null, string name = "SubmitOrder", string? file = null) => new()
    {
        Id = new FactId { Value = id },
        Subject = (artifact ?? Command).Subject,
        Evidence = At(line),
        Definition = new ArtifactDeclarationDefinition { Artifact = artifact ?? Command, Name = name, File = file }
    };

    protected static PropertyDefinition Property(
        string name,
        string type,
        bool identifier = false,
        SubjectId? subject = null,
        ArtifactKind? kind = null) => new()
        {
            Name = name,
            IsIdentifier = identifier,
            Type = new TypeReferenceDefinition { Name = type, Subject = subject, TargetArtifactKind = kind }
        };

    protected static ArtifactMemberDeclarationFact Order(string id, int line, string member, int order) => new()
    {
        Id = new FactId { Value = id },
        Subject = CommandSubject,
        Evidence = At(line),
        Definition = new ArtifactMemberDeclarationDefinition { Member = Member(member), DeclarationOrder = order }
    };

    protected static ArtifactMemberTypeUseFact TypeUse(
        string id,
        int line,
        string member,
        string type,
        params TypeUseShapeKind[] shape) => TypeUse(id, line, member, type, null, shape);

    protected static ArtifactMemberTypeUseFact TypeUse(
        string id,
        int line,
        string member,
        string type,
        SubjectId? observed,
        params TypeUseShapeKind[] shape) => new()
        {
            Id = new FactId { Value = id },
            Subject = CommandSubject,
            Evidence = At(line),
            Definition = new ArtifactMemberTypeUseDefinition
            {
                Member = Member(member),
                Type = new TypeUseDefinition
                {
                    Name = type,
                    ObservedTypeSubject = observed,
                    Shape = shape.Length == 0 ? [TypeUseShapeKind.Named] : shape
                }
            }
        };

    protected static TypeUseBindingFact Binding(string id, int line, string member, ArtifactKey target) => new()
    {
        Id = new FactId { Value = id },
        Subject = CommandSubject,
        Evidence = At(line),
        Definition = new TypeUseBindingDefinition { Member = Member(member), Target = target }
    };

    protected static ArtifactMemberRoleFact Role(string id, int line, string member, ArtifactMemberRoleKind role) => new()
    {
        Id = new FactId { Value = id },
        Subject = CommandSubject,
        Evidence = At(line),
        Definition = new ArtifactMemberRoleDefinition { Member = Member(member), Role = role }
    };

    protected static GenerationFact[] ValidMember(string name, int order, int line, string type = "String") =>
    [
        Order($"{name}-order", line, name, order),
        TypeUse($"{name}-type", line + 1, name, type)
    ];

    protected static ArtifactMemberKey Member(string name) => new() { Artifact = Command, Name = name };

    protected static ResolvedApplicationGraph Resolve(IEnumerable<GenerationFact> facts) =>
        new GenerationResolver().Resolve([new AdapterContribution { Adapter = Adapter, Facts = [.. facts] }]);

    protected static string[] DiagnosticLines(ResolvedApplicationGraph graph) =>
    [
        .. graph.Diagnostics.Select(diagnostic => string.Join(
            '|',
            diagnostic.Code,
            diagnostic.Severity,
            diagnostic.Outcome,
            diagnostic.Subject?.Value,
            diagnostic.Message,
            string.Join(',', diagnostic.Facts.Select(fact => fact.Value)),
            Coordinates(diagnostic.Source)))
    ];

    protected static string[] ArtifactLines(ResolvedApplicationGraph graph) =>
    [
        .. graph.Artifacts.SelectMany(artifact => artifact.Variants.SelectMany(variant => new[]
        {
            $"{artifact.Key.Kind}|{artifact.Key.Subject.Value}|{variant.Definition.Name}|file={variant.Definition.File}|files={string.Join(',', variant.Files)}"
        }.Concat(variant.Definition.Properties.Select(property => string.Join(
            '|',
            property.Name,
            property.Type.Name,
            $"collection={property.Type.IsCollection}",
            $"optional={property.Type.IsOptional}",
            $"subject={property.Type.Subject?.Value}",
            $"kind={property.Type.TargetArtifactKind}",
            $"identifier={property.IsIdentifier}")))))
    ];

    static string Coordinates(SourceRange? source) => source is null
        ? "<none>"
        : $"{source.Path}:{source.StartLine}:{source.StartColumn}-{source.EndLine}:{source.EndColumn}";
}
