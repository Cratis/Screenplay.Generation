// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation.given;

public class a_derivation : Specification
{
    protected static readonly AdapterIdentity ApplicationAdapter = new() { Id = "application", Version = "1.0.0" };
    protected static readonly AdapterIdentity ConceptAdapter = new() { Id = "concepts", Version = "2.0.0" };
    protected static readonly SubjectId CommandSubject = new() { Value = "dotnet://Ordering/Commands.RegisterCustomer" };
    protected static readonly SubjectId ConceptSubject = new() { Value = "dotnet://Ordering/Concepts.CustomerCode" };
    protected static readonly ArtifactKey Command = new() { Subject = CommandSubject, Kind = ArtifactKind.Command };

    protected static ArtifactDeclarationFact CommandDeclaration(string suffix = "command") => new()
    {
        Id = Id(ApplicationAdapter, suffix),
        Subject = CommandSubject,
        Evidence = Evidence(ApplicationAdapter, "Commands/RegisterCustomer.cs", 1),
        Definition = new ArtifactDeclarationDefinition
        {
            Artifact = Command,
            Name = "RegisterCustomer",
            File = "Commands/RegisterCustomer.cs"
        }
    };

    protected static ArtifactMemberDeclarationFact MemberDeclaration(
        string name,
        int order,
        string suffix) => new()
    {
        Id = Id(ApplicationAdapter, suffix),
        Subject = CommandSubject,
        Evidence = Evidence(ApplicationAdapter, "Commands/RegisterCustomer.cs", order + 2),
        Definition = new ArtifactMemberDeclarationDefinition
        {
            Member = Member(name),
            DeclarationOrder = order
        }
    };

    protected static ArtifactMemberTypeUseFact TypeUse(
        string name,
        SubjectId? observedType,
        string suffix,
        params TypeUseShapeKind[] shape) => new()
    {
        Id = Id(ApplicationAdapter, suffix),
        Subject = CommandSubject,
        Evidence = Evidence(ApplicationAdapter, "Commands/RegisterCustomer.cs", 10),
        Definition = new ArtifactMemberTypeUseDefinition
        {
            Member = Member(name),
            Type = new TypeUseDefinition
            {
                Name = "CustomerCode",
                ObservedTypeSubject = observedType,
                Shape = shape.Length == 0 ? [TypeUseShapeKind.Named] : shape
            }
        }
    };

    protected static ArtifactFact Concept(
        SubjectId subject,
        string suffix,
        string name = "CustomerCode") => new()
    {
        Id = Id(ConceptAdapter, suffix),
        Subject = subject,
        Evidence = Evidence(ConceptAdapter, $"Concepts/{suffix}.cs", 1),
        Definition = new ArtifactDefinition
        {
            Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept },
            Name = name,
            File = $"Concepts/{suffix}.cs"
        }
    };

    protected static ArtifactMemberKey Member(string name) => new()
    {
        Artifact = Command,
        Name = name
    };

    protected static GenerationDerivationSnapshot Derive(params GenerationFact[] facts) =>
        GenerationFactDerivation.Derive(new AdapterRunSnapshot
        {
            Facts = [.. facts.Select(fact => new GenerationFactRecord { Fact = fact })]
        });

    protected static string Projection(object? value)
    {
        if (value is null)
        {
            return Node([null]);
        }

        if (value is string text)
        {
            return Node([typeof(string).FullName, text]);
        }

        var type = value.GetType();
        if (type.IsEnum || type.IsPrimitive || value is decimal)
        {
            return Node([type.FullName, Convert.ToString(value, CultureInfo.InvariantCulture)]);
        }

        if (value is IEnumerable enumerable)
        {
            return Node([type.FullName, .. enumerable.Cast<object?>().Select(Projection)]);
        }

        var properties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name, StringComparer.Ordinal);
        return Node(
        [
            type.FullName,
            .. properties.Select(property => Node([property.Name, Projection(property.GetValue(value))]))
        ]);
    }

    static FactId Id(AdapterIdentity adapter, string suffix) => new() { Value = $"{adapter.Id}:{suffix}" };

    static Evidence Evidence(AdapterIdentity adapter, string path, int line) => new()
    {
        Adapter = adapter,
        Strength = EvidenceStrength.Exact,
        Source = new SourceRange
        {
            Path = path,
            StartLine = line,
            StartColumn = 1,
            EndLine = line,
            EndColumn = 20
        }
    };

    static string Node(IEnumerable<string?> values) => string.Concat(values.Select(value => value is null
        ? "-1:"
        : $"{value.Length.ToString(CultureInfo.InvariantCulture)}:{value}"));
}
